using Microsoft.AspNetCore.Identity;
using SkillHub.Application.Common.Interfaces;
using SkillHub.Application.DTOs;
using SkillHub.Application.Services.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new DomainException($"El correo '{request.Email}' ya se encuentra registrado.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Specialty = request.Specialty,
            Role = "Participant",
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new DomainException($"Error al registrar el participante: {errors}");
        }

        await _userManager.AddToRoleAsync(user, "Participant");

        var token = _jwtTokenGenerator.GenerateToken(user, "Participant");

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = "Participant",
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new DomainException("Credenciales inválidas. Verifique correo y contraseña.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? user.Role ?? "Participant";

        var token = _jwtTokenGenerator.GenerateToken(user, role);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = role,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
    }
}
