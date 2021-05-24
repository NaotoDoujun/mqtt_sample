using System.Linq;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Common;
namespace Bff.Models
{
  public class Query
  {
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Log> GetLogs([Service] ApplicationDbContext context) => context.Logs.AsQueryable();

    [UseOffsetPaging(IncludeTotalCount = true)]
    public IQueryable<Counter> GetLatests([Service] ApplicationDbContext context)
    {
      var subquery = from c in context.Counters
                     group c by c.NodeId into g
                     select new
                     {
                       NodeId = g.Key,
                       RecordTime = g.Max(a => a.RecordTime)
                     };
      var query = from c in context.Counters
                  join s in subquery
                    on c.NodeId equals s.NodeId
                  where c.RecordTime == s.RecordTime
                  orderby c.NodeId ascending
                  select c;
      return query;
    }

    [UseFiltering]
    [UseSorting]
    public IQueryable<Log> GetLogsForChart([Service] ApplicationDbContext context) => context.Logs.AsQueryable();
  }
}