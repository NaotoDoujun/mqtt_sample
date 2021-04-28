using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Subscriptions;
using Grpc.Core;
using Common.Proto;
using Bff.Models;
namespace Bff.Services
{
  public class GrpcService : CounterProto.CounterProtoBase
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GrpcService> _logger;
    private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

    public GrpcService(IServiceScopeFactory scopeFactory, ILogger<GrpcService> logger)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
    }

    public override async Task<Empty> Count(IAsyncStreamReader<Counters> stream, ServerCallContext context)
    {
      using var scope = _scopeFactory.CreateScope();
      List<Common.Proto.Counter> counters = new List<Common.Proto.Counter>();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      var eventSender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        while (await stream.MoveNext(CancellationToken.None))
        {
          var request = stream.Current;
          var counter = request.Counter;
          counters.AddRange(counter);
        }
        if (counters.Count > 1) _logger.LogInformation("counters size: {Count}", counters.Count);
        foreach (var c in counters)
        {
          await dbContext.Counters.AddAsync(new Bff.Models.Counter
          {
            NodeId = c.NodeId,
            Count = c.Count,
            RecordTime = c.RecordTime.ToDateTime()
          });
          _logger.LogInformation("[gRPC] Count: {Count}, RecordTime: {RecordTime}", c.Count, c.RecordTime);
        }
        await dbContext.SaveChangesAsync();
        if (dbContext.Counters.Count() > 0) await eventSender.SendAsync("ReturnedCounter", dbContext.Counters.OrderByDescending(c => c.RecordTime).FirstOrDefault());
        return new Empty();
      }
      finally
      {
        Semaphore.Release();
      }
    }

    public override async Task<Empty> FileSend(IAsyncStreamReader<Chunk> stream, ServerCallContext context)
    {
      List<byte> bytes = new List<byte>();
      while (await stream.MoveNext(CancellationToken.None))
      {
        var request = stream.Current;
        var temp = request.Chunk_.ToByteArray();
        bytes.AddRange(temp);
      }

      _logger.LogInformation("file size: {Count}", bytes.Count);
      //_logger.LogInformation(BitConverter.ToString(bytes.ToArray()));
      return new Empty();
    }
  }
}