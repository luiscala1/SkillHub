namespace SkillHub.Application.Services.Interfaces;

public interface IPdfService
{
    Task<byte[]> GenerateEnrollmentCertificatePdfAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
}
