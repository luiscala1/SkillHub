using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Common.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Infrastructure.Data;

namespace SkillHub.Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ApplicationDbContext _context;

    public EnrollmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments.FindAsync(new object?[] { id }, cancellationToken);
    }

    public async Task<Enrollment?> GetByIdWithWorkshopAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
                .ThenInclude(w => w.Instructor)
            .Include(e => e.Participant)
            .Include(e => e.CancelledByUser)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Enrollment?> GetByWorkshopAndParticipantAsync(
        Guid workshopId,
        string participantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
            .FirstOrDefaultAsync(e => e.WorkshopId == workshopId && e.ParticipantId == participantId, cancellationToken);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByWorkshopIdAsync(
        Guid workshopId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Participant)
            .Include(e => e.CancelledByUser)
            .Where(e => e.WorkshopId == workshopId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Enrollment>> GetAllByParticipantAsync(
        string participantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
                .ThenInclude(w => w.Instructor)
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Enrollment>> GetActiveByParticipantAsync(
        string participantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
            .Where(e => e.ParticipantId == participantId && e.Status == EnrollmentStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        await _context.Enrollments.AddAsync(enrollment, cancellationToken);
    }

    public Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        _context.Enrollments.Update(enrollment);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
