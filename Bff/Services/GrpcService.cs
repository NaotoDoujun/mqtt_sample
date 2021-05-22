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
        var counters = new List<Common.Proto.CounterRequest>();
        var logs = new List<Common.Log>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventSender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
        // logs keep 3min records
        var limit = DateTime.Now.AddMilliseconds(-180000).ToUniversalTime();
        dbContext.Logs.RemoveRange(dbContext.Logs.Where(c => c.RecordTime <= limit));
        await foreach (var message in stream.ReadAllAsync())
        {
          counters.AddRange(message.Counter);
        }
        var latest = new Common.Counter();
        foreach (var c in counters)
        {
          latest.NodeId = c.NodeId;
          latest.Count = c.Count;
          latest.RecordTime = c.RecordTime.ToDateTime();

          // save log
          logs.Add(new Common.Log
          {
            NodeId = latest.NodeId,
            Count = latest.Count,
            RecordTime = latest.RecordTime
          });

          await eventSender.SendAsync("ReturnedCounter", latest);
          _logger.LogInformation("[gRPC] Count: {Count}, RecordTime: {RecordTime}", latest.Count, latest.RecordTime.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
        }

        // record counter
        if (dbContext.Counters.Any(c => c.NodeId == latest.NodeId))
        {
          var target = dbContext.Counters.FirstOrDefault(c => c.NodeId == latest.NodeId);
          if (target != null)
          {
            target.Count = latest.Count;
            target.RecordTime = latest.RecordTime;
          }
        }
        else
        {
          if (!string.IsNullOrEmpty(latest.NodeId)) await dbContext.Counters.AddAsync(latest);
        }

        // record log
        await dbContext.Logs.AddRangeAsync(logs);
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