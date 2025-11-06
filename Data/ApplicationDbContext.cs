using Microsoft.EntityFrameworkCore;
using DingDingApp.Models;

namespace DingDingApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MessageLog> MessageLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Mobile).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(200);
            });

            modelBuilder.Entity<MessageLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MessageType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Content).IsRequired();
            });
        }
    }
}

