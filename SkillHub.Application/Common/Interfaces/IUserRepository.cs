using SkillHub.Domain.Entities;

namespace SkillHub.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
