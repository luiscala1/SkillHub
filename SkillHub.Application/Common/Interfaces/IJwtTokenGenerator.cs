using SkillHub.Domain.Entities;

namespace SkillHub.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user, string role);
}
