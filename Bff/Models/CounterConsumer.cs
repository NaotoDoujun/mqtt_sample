using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Subscriptions;
using Common;
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

        var counter = new Common.Counter
        {
          NodeId = context.Message.NodeId,
          Count = context.Message.Count,
          LocalRecordTime = context.Message.LocalRecordTime
        };
        // record counter
        var _counter = dbContext.Counters.FirstOrDefault(c => c.NodeId == counter.NodeId);
        if (_counter != null)
        {
          _counter.Count = counter.Count;
          _counter.LocalRecordTime = counter.LocalRecordTime;
        }
        else
        {
          await dbContext.Counters.AddAsync(counter);
        }

        // record log
        await dbContext.Logs.AddAsync(new Common.Log
        {
          NodeId = counter.NodeId,
          Count = counter.Count,
          LocalRecordTime = counter.LocalRecordTime
        });

        await dbContext.SaveChangesAsync();
        await eventSender.SendAsync("ReturnedCounter", counter);
        _logger.LogInformation("[AMQP] Count: {Count}, RecordTime: {RecordTime}", counter.Count, counter.LocalRecordTime.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
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
  }
}