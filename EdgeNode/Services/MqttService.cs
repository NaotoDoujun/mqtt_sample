using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Common;
using EdgeNode.Models;
namespace EdgeNode.Services
{
  public class MqttService : IHostedService, IDisposable
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IConfiguration _configuration;
    private readonly IServiceSettings _serviceSettings;
    private Timer _timer;
    private readonly IManagedMqttClient _client;
    private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    private int executionCount = 0;

    public MqttService(IServiceScopeFactory scopeFactory,
      ILogger<MqttService> logger,
      IHostApplicationLifetime appLifetime,
      IConfiguration configuration,
      IServiceSettings serviceSettings)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
      _appLifetime = appLifetime;
      _configuration = configuration;
      _serviceSettings = serviceSettings;
      _client = new MqttFactory().CreateManagedMqttClient();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _appLifetime.ApplicationStarted.Register(OnStarted);
      _appLifetime.ApplicationStopping.Register(OnStopping);
      _appLifetime.ApplicationStopped.Register(OnStopped);
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }

    private async void OnStarted()
    {
      var brokerSettings = _configuration.GetSection("BrokerSettings").Get<BrokerSettings>();
      var options = new ManagedMqttClientOptionsBuilder()
        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
        .WithClientOptions(new MqttClientOptionsBuilder()
            .WithClientId(_serviceSettings.NodeId)
            .WithTcpServer(brokerSettings.Host, brokerSettings.Port)
            .WithCredentials(brokerSettings.Username, brokerSettings.Password)
            .WithCleanSession(false)
            .Build())
          .Build();

      _client.UseConnectedHandler(handler =>
      {
        _logger.LogInformation("Connected successfully with MQTT Brokers.");
      });

      _client.UseDisconnectedHandler(handler =>
      {
        _logger.LogWarning("Disconnected from MQTT Brokers.");
      });

      await _client.StartAsync(options);
      _timer = new Timer(HandleTimerCallback, null, TimeSpan.Zero,
        TimeSpan.FromMilliseconds(_serviceSettings.TimeSpan));
    }

    private async void HandleTimerCallback(object state)
    {
      await CountAsync();
    }

    private async Task CountAsync()
    {
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (executionCount >= 100) executionCount = 0;
        var count = Interlocked.Increment(ref executionCount);
        _logger.LogTrace("[MQTT] Counter is working. Count: {Count}", count);
        var counters = dbContext.Counters.ToList();
        counters.Add(new Common.Counter
        {
          NodeId = _serviceSettings.NodeId,
          Count = count,
          RecordTime = DateTime.UtcNow
        });
        if (_client.IsConnected)
        {
          var message = new MqttApplicationMessageBuilder()
          .WithTopic("/count")
          .WithPayload(JsonSerializer.Serialize(counters.AsEnumerable()))
          .WithAtLeastOnceQoS()
          .Build();
          await _client.PublishAsync(message, CancellationToken.None);
          if (dbContext.Counters.Any())
          {
            _logger.LogTrace("[MQTT] succeeded. going to delete localDb records");
            dbContext.Counters.RemoveRange(dbContext.Counters);
            await dbContext.SaveChangesAsync();
          }
        }
        else
        {
          await dbContext.Counters.AddAsync(new Common.Counter
          {
            NodeId = _serviceSettings.NodeId,
            Count = count,
            RecordTime = DateTime.UtcNow
          });
          await dbContext.SaveChangesAsync();
          _logger.LogWarning("[MQTT] failed. Recorded to localDb: {Count}", count);
        }
      }
      finally
      {
        Semaphore.Release();
      }
    }

    private async void OnStopping()
    {
      _timer?.Change(Timeout.Infinite, 0);
      await _client?.StopAsync();
    }

    private void OnStopped()
    {
      this.Dispose();
    }

    public void Dispose()
    {
      Semaphore?.Dispose();
      _timer?.Dispose();
      _client?.Dispose();
    }
  }
}