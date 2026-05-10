using API_DASM.Models;
using Microsoft.EntityFrameworkCore;

namespace API_DASM.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<AccessLevel> AccessLevels { get; set; }
    }
}
