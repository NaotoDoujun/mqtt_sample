using Microsoft.EntityFrameworkCore;
using Common;
namespace EdgeNode.Models
{

  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Counter> Counters { get; set; }
  }
}