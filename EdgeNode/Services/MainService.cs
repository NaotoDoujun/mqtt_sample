using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.Client;
using MassTransit;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using EdgeNode.Models;
using Common.Proto;
namespace EdgeNode.Services
{
  public class MainService : IHostedService, IDisposable
  {
    private readonly IBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MainService> _logger;
    private Timer _timer;
    private GrpcChannel _channel;
    private IManagedMqttClient _mqttClient;
    private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    private int executionCountGRPC = 0;
    private int executionCountAMQP = 0;
    private int executionCountMQTT = 0;
    private readonly int timespan = 1000;
    private readonly string nodeId = Guid.NewGuid().ToString();

    public MainService(IBus bus, IServiceScopeFactory scopeFactory, ILogger<MainService> logger)
    {
      _bus = bus;
      _scopeFactory = scopeFactory;
      _logger = logger;
      _channel = GrpcChannel.ForAddress("https://bff.local:5001");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("Main Service running.");
      _mqttClient = new MqttFactory().CreateManagedMqttClient();
      var options = new ManagedMqttClientOptionsBuilder()
        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
        .WithClientOptions(new MqttClientOptionsBuilder()
            .WithClientId(nodeId)
            .WithTcpServer("broker.local", 1883)
            .WithCredentials("rabbitmq", "rabbitmq")
            .Build())
        .Build();
      await _mqttClient.StartAsync(options);
      _timer = new Timer(HandleTimerCallback, null, TimeSpan.Zero,
          TimeSpan.FromMilliseconds(timespan));
    }

    private async void HandleTimerCallback(object state)
    {
      await CountAsGRPCAsync();
      //await CountAsAMQPAsync();
      //await CountAsMQTTAsync();
    }

    // gRPC
    private async Task CountAsGRPCAsync()
    {
      using var scope = _scopeFactory.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var client = new CounterProto.CounterProtoClient(_channel);
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        if (executionCountGRPC >= 100) executionCountGRPC = 0;
        var count = Interlocked.Increment(ref executionCountGRPC);
        _logger.LogInformation("[gRPC] Counter is working. Count: {Count}", count);
        try
        {
          var query = dbContext.Counters.AsEnumerable();
          var sendCounters = new List<Common.Proto.Counter>();
          foreach (var record in query)
          {
            sendCounters.Add(new Common.Proto.Counter
            {
              NodeId = record.NodeId,
              Count = record.Count,
              RecordTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(record.RecordTime.ToUniversalTime())
            });
          }
          sendCounters.Add(new Common.Proto.Counter
          {
            NodeId = nodeId,
            Count = count,
            RecordTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.ToUniversalTime())
          });
          var stream = client.Count();
          await stream.RequestStream.WriteAsync(new Counters
          {
            Counter = { sendCounters }
          });
          await stream.RequestStream.CompleteAsync();
          var reply = await stream.ResponseAsync;
          if (reply is Common.Proto.Empty && dbContext.Counters.Count() > 0)
          {
            _logger.LogInformation("[gRPC] succeeded. going to delete localDb records");
            dbContext.Counters.RemoveRange(query);
            await dbContext.SaveChangesAsync();
          }
        }
        catch
        {
          var counter = new EdgeNode.Models.Counter
          {
            NodeId = nodeId,
            Count = count,
            RecordTime = DateTime.Now
          };
          await dbContext.Counters.AddAsync(counter);
          await dbContext.SaveChangesAsync();
          _logger.LogWarning("[gRPC] failed. so, recorded to localDb: {Counter}", counter);
        }

      }
      finally
      {
        Semaphore.Release();
      }
    }

    // AMQP
    private async Task CountAsAMQPAsync()
    {
      using var scope = _scopeFactory.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        if (executionCountAMQP >= 100) executionCountAMQP = 0;
        var count = Interlocked.Increment(ref executionCountAMQP);
        _logger.LogInformation("[AMQP] Counter is working. Count: {Count}", count);
        var counter = new EdgeNode.Models.Counter
        {
          NodeId = nodeId,
          Count = count,
          RecordTime = DateTime.Now.ToUniversalTime()
        };
        try
        {
          var counters = dbContext.Counters.ToList();
          counters.Add(counter);
          foreach (var record in counters)
          {
            await _bus.Publish(record);
          }
          if (dbContext.Counters.Count() > 0)
          {
            _logger.LogInformation("[AMQP] succeeded. going to delete localDb records");
            dbContext.Counters.RemoveRange(dbContext.Counters.AsEnumerable());
            await dbContext.SaveChangesAsync();
          }
        }
        catch
        {
          await dbContext.Counters.AddAsync(counter);
          await dbContext.SaveChangesAsync();
          _logger.LogWarning("[AMQP] failed. so, recorded to localDb: {Counter}", counter);
        }
      }
      finally
      {
        Semaphore.Release();
      }
    }

    // MQTT
    private async Task CountAsMQTTAsync()
    {
      using var scope = _scopeFactory.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {

        if (executionCountMQTT >= 100) executionCountMQTT = 0;
        var count = Interlocked.Increment(ref executionCountMQTT);
        _logger.LogInformation("[MQTT] Counter is working. Count: {Count}", count);
        var counter = new EdgeNode.Models.Counter
        {
          NodeId = nodeId,
          Count = count,
          RecordTime = DateTime.Now.ToUniversalTime()
        };
        try
        {
          var counters = dbContext.Counters.ToList();
          counters.Add(counter);
          foreach (var record in counters)
          {
            await _mqttClient.PublishAsync(new ManagedMqttApplicationMessageBuilder().WithApplicationMessage(msg =>
            {
              msg.WithTopic("/topic")
              .WithPayload(JsonSerializer.Serialize(counter))
              .WithAtLeastOnceQoS();
            }).Build());
          }
          if (dbContext.Counters.Count() > 0)
          {
            _logger.LogInformation("[MQTT] succeeded. going to delete localDb records");
            dbContext.Counters.RemoveRange(dbContext.Counters.AsEnumerable());
            await dbContext.SaveChangesAsync();
          }
        }
        catch
        {
          await dbContext.Counters.AddAsync(counter);
          await dbContext.SaveChangesAsync();
          _logger.LogWarning("[MQTT] failed. so, recorded to localDb: {Counter}", counter);
        }

      }
      finally
      {
        Semaphore.Release();
      }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("Main Service is stopping.");
      _timer?.Change(Timeout.Infinite, 0);
      await _channel?.ShutdownAsync();
      await _mqttClient?.StopAsync();
    }

    public void Dispose()
    {
      _timer?.Dispose();
      _channel?.Dispose();
      _mqttClient?.Dispose();
    }

  }
}