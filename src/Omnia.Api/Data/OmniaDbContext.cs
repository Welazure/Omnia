using Microsoft.EntityFrameworkCore;

namespace Omnia.Api.Data;

public sealed class OmniaDbContext(DbContextOptions<OmniaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Clip> Clips => Set<Clip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("Users");
            user.HasKey(u => u.Id);
            user.Property(u => u.Email)
                .HasConversion(email => email.ToLowerInvariant(), email => email);
            user.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Clip>(clip =>
        {
            clip.ToTable("Clips");
            clip.HasKey(c => c.Id);
            clip.Property(c => c.Content).IsRequired();
            clip.HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            clip.HasIndex(c => c.UserId);
        });
    }
}
