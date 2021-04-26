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
      using var scope = _scopeFactory.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var eventSender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        var counter = new Counter
        {
          NodeId = context.Message.NodeId,
          Count = context.Message.Count,
          RecordTime = context.Message.RecordTime
        };
        await dbContext.Counters.AddAsync(counter);
        await dbContext.SaveChangesAsync();
        if (dbContext.Counters.Count() > 0) await eventSender.SendAsync("ReturnedCounter", dbContext.Counters.OrderByDescending(c => c.RecordTime).FirstOrDefault());
        _logger.LogInformation("[AMQP] Count: {Count}, RecordTime: {RecordTime}", counter.Count, counter.RecordTime);
      }
      finally
      {
        Semaphore.Release();
      }
    }
  }
}