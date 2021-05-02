using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Subscriptions;
namespace Bff.Models
{
  public class CounterConsumer : IConsumer<Common.ICounter>
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CounterConsumer> _logger;
    private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

    public CounterConsumer(IServiceScopeFactory scopeFactory, ILogger<CounterConsumer> logger)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
    }

    public async Task Consume(ConsumeContext<Common.ICounter> context)
    {
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventSender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
        var counter = new Counter
        {
          NodeId = context.Message.NodeId,
          Count = context.Message.Count,
          RecordTime = context.Message.RecordTime
        };
        // keep 3min records
        var limit = DateTime.Now.AddMilliseconds(-180000).ToUniversalTime();
        var over = dbContext.Counters.Where(c => c.RecordTime <= limit).ToArray();
        if (over.Length > 0) dbContext.Counters.RemoveRange(over);
        await dbContext.Counters.AddAsync(counter);
        await dbContext.SaveChangesAsync();
        await eventSender.SendAsync("ReturnedCounter", counter);
        _logger.LogInformation("[AMQP] Count: {Count}, RecordTime: {RecordTime}", counter.Count, counter.RecordTime.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
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
  }
}