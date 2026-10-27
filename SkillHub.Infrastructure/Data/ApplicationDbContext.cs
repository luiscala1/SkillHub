using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillHub.Domain.Entities;

namespace SkillHub.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Workshop> Workshops => Set<Workshop>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Workshop>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.Property(w => w.Code)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(w => w.Code)
                .IsUnique();

            entity.Property(w => w.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(w => w.Description)
                .HasMaxLength(500);

            entity.Property(w => w.MaxCapacity)
                .IsRequired();

            entity.HasOne(w => w.Instructor)
                .WithMany(u => u.InstructedWorkshops)
                .HasForeignKey(w => w.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(w => w.Enrollments)
                .WithOne(e => e.Workshop)
                .HasForeignKey(e => e.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.HasIndex(e => new { e.WorkshopId, e.ParticipantId });

            entity.HasOne(e => e.Participant)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CancelledByUser)
                .WithMany()
                .HasForeignKey(e => e.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(u => u.Specialty)
                .HasMaxLength(100);

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(30);
        });
    }
}
