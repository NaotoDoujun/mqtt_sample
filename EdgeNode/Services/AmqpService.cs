using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using MassTransit.Monitoring.Health;
using Common;
using EdgeNode.Models;
namespace EdgeNode.Services
{
  public class AmqpService : IHostedService, IDisposable
  {
    private readonly IBus _bus;
    private readonly IBusHealth _busHealth;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AmqpService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IConfiguration _configuration;
    private readonly IServiceSettings _serviceSettings;
    private Timer _timer;
    private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    private int executionCount = 0;

    public AmqpService(
      IBus bus,
      IBusHealth busHealth,
      IServiceScopeFactory scopeFactory,
      ILogger<AmqpService> logger,
      IHostApplicationLifetime appLifetime,
      IConfiguration configuration,
      IServiceSettings serviceSettings)
    {
      _bus = bus;
      _busHealth = busHealth;
      _scopeFactory = scopeFactory;
      _logger = logger;
      _appLifetime = appLifetime;
      _configuration = configuration;
      _serviceSettings = serviceSettings;
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
      using var scope = _scopeFactory.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        if (executionCount >= 100) executionCount = 0;
        var count = Interlocked.Increment(ref executionCount);
        _logger.LogInformation("[AMQP] Counter is working. Count: {Count}", count);
        var counter = new Counter
        {
          NodeId = _serviceSettings.NodeId,
          Count = count,
          LocalRecordTime = DateTime.Now
        };
        var health = _busHealth.CheckHealth();
        if (health.Description == "Ready")
        {
          var counters = dbContext.Counters.ToList();
          counters.Add(counter);
          foreach (var record in counters)
          {
            await _bus.Publish(record);
          }
          if (dbContext.Counters.Any())
          {
            _logger.LogInformation("[AMQP] succeeded. going to delete localDb records");
            dbContext.Counters.RemoveRange(dbContext.Counters.AsEnumerable());
            await dbContext.SaveChangesAsync();
          }
        }
        else
        {
          await dbContext.Counters.AddAsync(counter);
          await dbContext.SaveChangesAsync();
          _logger.LogWarning("[AMQP] failed. so, recorded to localDb: {Count}", counter.Count);
        }
      }
      finally
      {
        Semaphore.Release();
      }
    }

    private void OnStopping()
    {
      _timer?.Change(Timeout.Infinite, 0);
    }

    private void OnStopped()
    {
      this.Dispose();
    }

    public void Dispose()
    {
      Semaphore?.Dispose();
      _timer?.Dispose();
    }
  }
}