using System.Linq;
using HotChocolate;
namespace Bff.Models
{
  public class Query
  {
    public IQueryable<Counter> GetCounters([Service] ApplicationDbContext context)
    {
      return context.Counters.AsQueryable();
    }

    public IQueryable<Counter> GetCounter(int id, [Service] ApplicationDbContext context)
    {
      return context.Counters.Where(counter => counter.Id == id);
    }

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
                  select c;
      return query;
    }
  }
}