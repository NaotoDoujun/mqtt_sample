using System.Linq;
using HotChocolate;
using Common;
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
  }
}