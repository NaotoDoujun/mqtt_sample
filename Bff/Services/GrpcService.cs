using System;
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

    public GrpcService(IServiceScopeFactory scopeFactory,
      ILogger<GrpcService> logger)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
    }

    public override async Task<Common.Proto.CounterReply> Count(IAsyncStreamReader<Common.Proto.CounterRequests> stream, ServerCallContext context)
    {
      await Semaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        using var scope = _scopeFactory.CreateScope();
        List<Common.Proto.CounterRequest> counters = new List<Common.Proto.CounterRequest>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventSender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
        // keep 3min records
        var limit = DateTime.Now.AddMilliseconds(-180000).ToUniversalTime();
        var over = dbContext.Counters.Where(c => c.RecordTime <= limit).ToArray();
        if (over.Length > 0) dbContext.Counters.RemoveRange(over);
        await foreach (var message in stream.ReadAllAsync())
        {
          counters.AddRange(message.Counter);
        }
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
          _logger.LogInformation("[gRPC] Count: {Count}, RecordTime: {RecordTime}", counter.Count, counter.RecordTime);
        }
        await dbContext.SaveChangesAsync();
        return new Common.Proto.CounterReply
        {
          MessageType = Common.Proto.Type.Success
        };
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
        return new Common.Proto.CounterReply
        {
          MessageType = Common.Proto.Type.Failure,
          Message = e.Message
        };
      }
      finally
      {
        Semaphore.Release();
      }
    }

    public override async Task<Google.Protobuf.WellKnownTypes.Empty> FileSend(IAsyncStreamReader<Common.Proto.Chunk> stream, ServerCallContext context)
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
      return new Google.Protobuf.WellKnownTypes.Empty();
    }
  }
}