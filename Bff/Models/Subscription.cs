using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Common;
namespace Bff.Models
{
  public class Subscription
  {

    [SubscribeAndResolve]
    public async ValueTask<ISourceStream<Counter>> OnRecorded([Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
      return await eventReceiver.SubscribeAsync<string, Counter>("ReturnedCounter", cancellationToken);
    }
  }
}