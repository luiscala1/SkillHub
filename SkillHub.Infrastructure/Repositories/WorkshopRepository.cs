using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Common.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Data;

namespace SkillHub.Infrastructure.Repositories;

public class WorkshopRepository : IWorkshopRepository
{
    private readonly ApplicationDbContext _context;

    public WorkshopRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Workshop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Workshops.FindAsync(new object?[] { id }, cancellationToken);
    }

    public async Task<Workshop?> GetByIdWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Workshops
            .Include(w => w.Instructor)
            .Include(w => w.Enrollments)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Workshop?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Workshops
            .Include(w => w.Instructor)
            .Include(w => w.Enrollments)
            .FirstOrDefaultAsync(w => w.Code == code, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Workshops.AnyAsync(w => w.Code.ToLower() == code.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<Workshop>> GetFutureWorkshopsAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        return await _context.Workshops
            .Include(w => w.Instructor)
            .Include(w => w.Enrollments)
            .Where(w => w.StartDateTime > utcNow)
            .OrderBy(w => w.StartDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Workshop>> GetTopWorkshopsByActiveEnrollmentsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Workshops
            .Include(w => w.Instructor)
            .Include(w => w.Enrollments)
            .OrderByDescending(w => w.Enrollments.Count(e => e.Status == Domain.Enums.EnrollmentStatus.Active))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Workshop workshop, CancellationToken cancellationToken = default)
    {
        await _context.Workshops.AddAsync(workshop, cancellationToken);
    }

    public Task UpdateAsync(Workshop workshop, CancellationToken cancellationToken = default)
    {
        _context.Workshops.Update(workshop);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
