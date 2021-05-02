using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using HotChocolate.Subscriptions;
using Common;
using Bff.Models;
namespace Bff.Services
{
  public class MqttService : IHostedService, IDisposable
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IConfiguration _configuration;
    private readonly IManagedMqttClient _client;
    private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

    public MqttService(IServiceScopeFactory scopeFactory,
      ILogger<MqttService> logger,
      IHostApplicationLifetime appLifetime,
      IConfiguration configuration)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
      _appLifetime = appLifetime;
      _configuration = configuration;
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
            .WithClientId("bff")
            .WithTcpServer(brokerSettings.Host, brokerSettings.Port)
            .WithCredentials(brokerSettings.Username, brokerSettings.Password)
            .WithCleanSession(false)
            .Build())
          .Build();

      _client.UseConnectedHandler(async handler =>
      {
        _logger.LogInformation("Connected successfully with MQTT Brokers.");
        await _client.SubscribeAsync(new MqttTopicFilterBuilder()
          .WithTopic("/topic")
          .WithAtLeastOnceQoS()
          .Build());
      });

      _client.UseApplicationMessageReceivedHandler(async handler =>
      {
        await SubscribeAsync(handler);
      });
      await _client.StartAsync(options);
    }

    private async Task SubscribeAsync(MqttApplicationMessageReceivedEventArgs handler)
    {
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventSender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
        var appMessage = handler.ApplicationMessage;
        // keep 3min records
        var limit = DateTime.Now.AddMilliseconds(-180000).ToUniversalTime();
        var over = dbContext.Counters.Where(c => c.RecordTime <= limit).ToArray();
        if (over.Length > 0) dbContext.Counters.RemoveRange(over);

        var payload = Encoding.UTF8.GetString(appMessage.Payload, 0, appMessage.Payload.Length);
        var counters = JsonSerializer.Deserialize<IEnumerable<Common.Proto.CounterRequest>>(payload);
        foreach (var c in counters)
        {
          var counter = new Bff.Models.Counter
          {
            NodeId = c.NodeId,
            Count = c.Count,
            RecordTime = c.RecordTime.ToDateTime()
          };
          await eventSender.SendAsync("ReturnedCounter", counter);
          await dbContext.Counters.AddAsync(counter);
          _logger.LogInformation("[MQTT] Count: {Count}, RecordTime: {RecordTime}", counter.Count, counter.RecordTime);
        }
        await dbContext.SaveChangesAsync();
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
      }
      finally
      {
        Semaphore.Release();
      }
    }

    private async void OnStopping()
    {
      await _client?.StopAsync();
    }

    private void OnStopped()
    {
      this.Dispose();
    }

    public void Dispose()
    {
      Semaphore?.Dispose();
      _client?.Dispose();
    }
  }
}