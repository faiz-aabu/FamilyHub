using FamilyHub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Data;

/// <summary>
/// Represents the Entity Framework Core database context for FamilyHub.
/// This class connects the application models to the database,
/// including ASP.NET Core Identity tables.
/// </summary>
public class FamilyHubDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Creates a new database context instance.
    /// </summary>
    /// <param name="options">Database configuration options.</param>
    public FamilyHubDbContext(DbContextOptions<FamilyHubDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Family members table.
    /// </summary>
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();

    /// <summary>
    /// Family relationships table.
    /// </summary>
    public DbSet<FamilyRelationship> FamilyRelationships => Set<FamilyRelationship>();

    /// <summary>
    /// Activity log entries table.
    /// </summary>
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    /// <summary>
    /// User notifications table.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Configures database relationships and rules.
    /// </summary>
    /// <param name="modelBuilder">Used to configure entity mappings.</param>
   protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);


    modelBuilder.Entity<FamilyRelationship>()
        .HasOne(fr => fr.Member)
        .WithMany(fm => fm.Relationships)
        .HasForeignKey(fr => fr.MemberId)
        .OnDelete(DeleteBehavior.Restrict);


    modelBuilder.Entity<FamilyRelationship>()
        .HasOne(fr => fr.RelatedMember)
        .WithMany()
        .HasForeignKey(fr => fr.RelatedMemberId)
        .OnDelete(DeleteBehavior.Restrict);
}
    }
