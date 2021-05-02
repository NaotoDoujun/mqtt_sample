using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.Client;
using EdgeNode.Models;
using Common;
using Common.Proto;
namespace EdgeNode.Services
{
  public class GrpcService : IHostedService, IDisposable
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GrpcService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IServiceSettings _serviceSettings;
    private Timer _timer;
    private readonly GrpcChannel _channel;
    private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    private int executionCount = 0;

    public GrpcService(IServiceScopeFactory scopeFactory,
      ILogger<GrpcService> logger,
      IHostApplicationLifetime appLifetime,
      IConfiguration configuration,
      IHostEnvironment environment,
      IServiceSettings serviceSettings)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
      _appLifetime = appLifetime;
      _serviceSettings = serviceSettings;

      if (environment.IsDevelopment()) AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

      var grpcSettings = configuration.GetSection("GrpcSettings").Get<GrpcSettings>();
      _channel = GrpcChannel.ForAddress(grpcSettings.Channel);
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

    private void OnStarted()
    {
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
        var client = new CounterProto.CounterProtoClient(_channel);
        if (executionCount >= 100) executionCount = 0;
        var count = Interlocked.Increment(ref executionCount);
        _logger.LogInformation("[gRPC] Counter is working. Count: {Count}", count);
        try
        {
          var query = dbContext.Counters.AsEnumerable();
          var sendCounters = new List<Common.Proto.CounterRequest>();
          foreach (var record in query)
          {
            sendCounters.Add(new Common.Proto.CounterRequest
            {
              NodeId = record.NodeId,
              Count = record.Count,
              RecordTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(record.RecordTime.ToUniversalTime())
            });
          }
          sendCounters.Add(new Common.Proto.CounterRequest
          {
            NodeId = _serviceSettings.NodeId,
            Count = count,
            RecordTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now.ToUniversalTime())
          });
          var stream = client.Count();
          await stream.RequestStream.WriteAsync(new CounterRequests
          {
            Counter = { sendCounters }
          });
          await stream.RequestStream.CompleteAsync();
          var reply = await stream.ResponseAsync;
          if (reply.MessageType == Common.Proto.Type.Success)
          {
            if (dbContext.Counters.Count() > 0)
            {
              _logger.LogInformation("[gRPC] succeeded. going to delete localDb records");
              dbContext.Counters.RemoveRange(query);
              await dbContext.SaveChangesAsync();
            }
          }
          else
          {
            _logger.LogError("[gRPC] got failed message from server. Message:{Message}", reply.Message);
          }
        }
        catch
        {
          var counter = new EdgeNode.Models.Counter
          {
            NodeId = _serviceSettings.NodeId,
            Count = count,
            RecordTime = DateTime.Now
          };
          await dbContext.Counters.AddAsync(counter);
          await dbContext.SaveChangesAsync();
          _logger.LogWarning("[gRPC] failed. Recorded to localDb: {Counter}", counter);
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
      await _channel?.ShutdownAsync();
    }

    private void OnStopped()
    {
      this.Dispose();
    }

    public void Dispose()
    {
      Semaphore?.Dispose();
      _timer?.Dispose();
      _channel?.Dispose();
    }
  }
}