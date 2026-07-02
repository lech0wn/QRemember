using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Models;

namespace QRemember.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Photo> Photos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Event>()
            .HasIndex(e => e.EventCode)
            .IsUnique();

        builder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(u => u.Events)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Photo>()
            .HasOne(p => p.Event)
            .WithMany(e => e.Photos)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}