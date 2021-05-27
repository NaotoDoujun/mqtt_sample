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
    private readonly SemaphoreSlim MovieSemaphore = new SemaphoreSlim(1, 1);

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
        var topicFilters = new List<MqttTopicFilter>();
        topicFilters.Add(new MqttTopicFilterBuilder()
          .WithTopic("/count")
          .WithAtLeastOnceQoS()
          .Build());
        topicFilters.Add(new MqttTopicFilterBuilder()
          .WithTopic("/putmovie")
          .WithAtMostOnceQoS()
          .Build());
        await _client.SubscribeAsync(topicFilters);
      });

      _client.UseApplicationMessageReceivedHandler(async handler =>
      {
        switch (handler.ApplicationMessage.Topic)
        {
          case "/putmovie":
            await SubscribeMovieAsync(handler);
            break;
          default:
            await SubscribeAsync(handler);
            break;
        }

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
        var payload = Encoding.UTF8.GetString(appMessage.Payload, 0, appMessage.Payload.Length);
        var counters = JsonSerializer.Deserialize<IEnumerable<Common.Counter>>(payload);
        if (counters.Any())
        {
          var latest = counters.Last();
          var logs = new List<Common.Log>();
          foreach (var c in counters)
          {
            // save log
            logs.Add(new Common.Log
            {
              NodeId = c.NodeId,
              Count = c.Count,
              LocalRecordTime = c.LocalRecordTime
            });
          }

          // record counter
          var _counter = dbContext.Counters.FirstOrDefault(c => c.NodeId == latest.NodeId);
          if (_counter != null)
          {
            _counter.Count = latest.Count;
            _counter.LocalRecordTime = latest.LocalRecordTime;
          }
          else
          {
            await dbContext.Counters.AddAsync(latest);
          }

          // record log
          await dbContext.Logs.AddRangeAsync(logs);
          await dbContext.SaveChangesAsync();

          await eventSender.SendAsync("ReturnedCounter", latest);
          _logger.LogInformation("[MQTT] Count: {Count}, RecordTime: {RecordTime}", latest.Count, latest.LocalRecordTime.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
        }
      }
      catch (Exception e)
      {
        _logger.LogError(e.StackTrace);
      }
      finally
      {
        Semaphore.Release();
      }
    }

    private async Task SubscribeMovieAsync(MqttApplicationMessageReceivedEventArgs handler)
    {
      await MovieSemaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        using var scope = _scopeFactory.CreateScope();
        var eventSender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
        var image = handler.ApplicationMessage.Payload;
        await eventSender.SendAsync("MovieFrameBase64", Convert.ToBase64String(image));
        _logger.LogTrace("[MQTT] Frame Image: {Image}", image);
      }
      finally
      {
        MovieSemaphore.Release();
      }
    }

    private async void OnStopping()
    {
      await _client?.StopAsync();
    }

    public void Dispose()
    {
      Semaphore?.Dispose();
      _client?.Dispose();
    }
  }
}