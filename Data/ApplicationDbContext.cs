using Elanco___Tick_Sighting.Models;
using Microsoft.EntityFrameworkCore;

namespace Elanco___Tick_Sighting.Data
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
        public DbSet<TickSighting> TickSightings { get; set; }
    }
}
