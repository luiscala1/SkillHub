using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Common.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Data;

namespace SkillHub.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail || u.Email == email, cancellationToken);
    }
}
