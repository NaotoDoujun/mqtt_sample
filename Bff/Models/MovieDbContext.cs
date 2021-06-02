using Microsoft.EntityFrameworkCore;
using Common;
namespace Bff.Models
{
  public class MovieDbContext : DbContext
  {
    public MovieDbContext(DbContextOptions<MovieDbContext> options)
        : base(options) { }

    public DbSet<Frame> Frames { get; set; }

  }
}