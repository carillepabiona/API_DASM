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

        public DbSet<Document> Documents { get; set; }

        public DbSet<DocumentVersion> DocumentVersions { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Folder> Folders { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DocumentVersion>()
                .HasOne(x => x.Document)
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(x => x.AccessLevel)
                .WithMany()
                .HasForeignKey(x => x.AccessLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActivityLog>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
