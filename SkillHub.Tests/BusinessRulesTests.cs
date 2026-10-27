using Moq;
using SkillHub.Application.Common.Interfaces;
using SkillHub.Application.DTOs;
using SkillHub.Application.Services.Implementations;
using SkillHub.Application.Services.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;
using Xunit;

namespace SkillHub.Tests;

public class BusinessRulesTests
{
    [Fact]
    public void SoloFuturos_WhenStartDateIsInThePast_ThrowsInvalidWorkshopDatesException()
    {
        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Code = "TEST-01",
            Title = "Taller Pasado",
            StartDateTime = DateTime.UtcNow.AddHours(-2),
            EndDateTime = DateTime.UtcNow.AddHours(2),
            MaxCapacity = 10
        };

        Assert.Throws<InvalidWorkshopDatesException>(() => workshop.ValidateDates(DateTime.UtcNow));
    }

    [Fact]
    public void SoloFuturos_WhenEndDateIsBeforeStartDate_ThrowsInvalidWorkshopDatesException()
    {
        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Code = "TEST-01",
            Title = "Taller Fechas Mal",
            StartDateTime = DateTime.UtcNow.AddDays(2),
            EndDateTime = DateTime.UtcNow.AddDays(1),
            MaxCapacity = 10
        };

        Assert.Throws<InvalidWorkshopDatesException>(() => workshop.ValidateDates(DateTime.UtcNow));
    }

    [Fact]
    public async Task CupoMaximo_WhenWorkshopIsFull_ThrowsWorkshopCapacityExceededException()
    {
        var workshopId = Guid.NewGuid();
        var workshop = new Workshop
        {
            Id = workshopId,
            Code = "TEST-FULL",
            Title = "Taller Lleno",
            StartDateTime = DateTime.UtcNow.AddDays(3),
            EndDateTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            MaxCapacity = 1
        };

        workshop.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(),
            WorkshopId = workshopId,
            ParticipantId = "user-1",
            Status = EnrollmentStatus.Active
        });

        var workshopRepo = new Mock<IWorkshopRepository>();
        workshopRepo.Setup(r => r.GetByIdWithEnrollmentsAsync(workshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        var enrollmentRepo = new Mock<IEnrollmentRepository>();

        var service = new EnrollmentService(workshopRepo.Object, enrollmentRepo.Object);

        await Assert.ThrowsAsync<WorkshopCapacityExceededException>(() =>
            service.EnrollParticipantAsync(new EnrollParticipantRequestDto
            {
                WorkshopId = workshopId,
                ParticipantId = "user-2"
            }));
    }

    [Fact]
    public async Task SinDuplicados_WhenActiveEnrollmentExists_ThrowsDuplicateEnrollmentException()
    {
        var workshopId = Guid.NewGuid();
        var participantId = "user-1";

        var workshop = new Workshop
        {
            Id = workshopId,
            Code = "TEST-DUP",
            Title = "Taller",
            StartDateTime = DateTime.UtcNow.AddDays(3),
            EndDateTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            MaxCapacity = 10
        };

        var existingEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            WorkshopId = workshopId,
            ParticipantId = participantId,
            Status = EnrollmentStatus.Active
        };

        var workshopRepo = new Mock<IWorkshopRepository>();
        workshopRepo.Setup(r => r.GetByIdWithEnrollmentsAsync(workshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        var enrollmentRepo = new Mock<IEnrollmentRepository>();
        enrollmentRepo.Setup(r => r.GetByWorkshopAndParticipantAsync(workshopId, participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEnrollment);

        var service = new EnrollmentService(workshopRepo.Object, enrollmentRepo.Object);

        await Assert.ThrowsAsync<DuplicateEnrollmentException>(() =>
            service.EnrollParticipantAsync(new EnrollParticipantRequestDto
            {
                WorkshopId = workshopId,
                ParticipantId = participantId
            }));
    }

    [Fact]
    public async Task CruceDeHorarios_WhenParticipantHasOverlappingWorkshop_ThrowsScheduleConflictException()
    {
        var workshopId = Guid.NewGuid();
        var participantId = "user-1";

        var newWorkshop = new Workshop
        {
            Id = workshopId,
            Code = "TEST-NEW",
            Title = "Taller Nuevo",
            StartDateTime = DateTime.UtcNow.AddDays(2).AddHours(10),
            EndDateTime = DateTime.UtcNow.AddDays(2).AddHours(12),
            MaxCapacity = 10
        };

        var existingWorkshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Code = "TEST-OLD",
            Title = "Taller Existente",
            StartDateTime = DateTime.UtcNow.AddDays(2).AddHours(11),
            EndDateTime = DateTime.UtcNow.AddDays(2).AddHours(13),
            MaxCapacity = 10
        };

        var activeEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            WorkshopId = existingWorkshop.Id,
            Workshop = existingWorkshop,
            ParticipantId = participantId,
            Status = EnrollmentStatus.Active
        };

        var workshopRepo = new Mock<IWorkshopRepository>();
        workshopRepo.Setup(r => r.GetByIdWithEnrollmentsAsync(workshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newWorkshop);

        var enrollmentRepo = new Mock<IEnrollmentRepository>();
        enrollmentRepo.Setup(r => r.GetByWorkshopAndParticipantAsync(workshopId, participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);
        enrollmentRepo.Setup(r => r.GetActiveByParticipantAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { activeEnrollment });

        var service = new EnrollmentService(workshopRepo.Object, enrollmentRepo.Object);

        await Assert.ThrowsAsync<ScheduleConflictException>(() =>
            service.EnrollParticipantAsync(new EnrollParticipantRequestDto
            {
                WorkshopId = workshopId,
                ParticipantId = participantId
            }));
    }

    [Fact]
    public void CancelacionParticipante_WhenLessThan24Hours_ThrowsEnrollmentCancellationNotAllowedException()
    {
        var workshop = new Workshop
        {
            StartDateTime = DateTime.UtcNow.AddHours(12)
        };

        var enrollment = new Enrollment
        {
            ParticipantId = "user-1",
            Workshop = workshop,
            Status = EnrollmentStatus.Active
        };

        Assert.Throws<EnrollmentCancellationNotAllowedException>(() =>
            enrollment.CancelByParticipant(DateTime.UtcNow));
    }

    [Fact]
    public void CancelacionParticipante_WhenMoreThan24Hours_CancelsSuccessfully()
    {
        var workshop = new Workshop
        {
            StartDateTime = DateTime.UtcNow.AddHours(30)
        };

        var enrollment = new Enrollment
        {
            ParticipantId = "user-1",
            Workshop = workshop,
            Status = EnrollmentStatus.Active
        };

        enrollment.CancelByParticipant(DateTime.UtcNow);

        Assert.Equal(EnrollmentStatus.Cancelled, enrollment.Status);
        Assert.NotNull(enrollment.CancelledAt);
        Assert.Equal("user-1", enrollment.CancelledByUserId);
    }

    [Fact]
    public void CancelacionEmpleado_WhenWorkshopHasNotStarted_CancelsEvenWithLessThan24Hours()
    {
        var workshop = new Workshop
        {
            StartDateTime = DateTime.UtcNow.AddHours(2) // less than 24h
        };

        var enrollment = new Enrollment
        {
            ParticipantId = "user-1",
            Workshop = workshop,
            Status = EnrollmentStatus.Active
        };

        enrollment.CancelByEmployee("admin-emp", DateTime.UtcNow);

        Assert.Equal(EnrollmentStatus.Cancelled, enrollment.Status);
        Assert.NotNull(enrollment.CancelledAt);
        Assert.Equal("admin-emp", enrollment.CancelledByUserId);
    }

    [Fact]
    public async Task Reinscripcion_WhenPreviousEnrollmentWasCancelled_ReactivatesEnrollment()
    {
        var workshopId = Guid.NewGuid();
        var participantId = "user-1";

        var workshop = new Workshop
        {
            Id = workshopId,
            Code = "TEST-RE",
            Title = "Taller Reinscripcion",
            StartDateTime = DateTime.UtcNow.AddDays(5),
            EndDateTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            MaxCapacity = 10
        };

        var cancelledEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            WorkshopId = workshopId,
            ParticipantId = participantId,
            Status = EnrollmentStatus.Cancelled,
            CancelledAt = DateTime.UtcNow.AddDays(-1),
            CancelledByUserId = participantId
        };

        var workshopRepo = new Mock<IWorkshopRepository>();
        workshopRepo.Setup(r => r.GetByIdWithEnrollmentsAsync(workshopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        var enrollmentRepo = new Mock<IEnrollmentRepository>();
        enrollmentRepo.Setup(r => r.GetByWorkshopAndParticipantAsync(workshopId, participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelledEnrollment);
        enrollmentRepo.Setup(r => r.GetActiveByParticipantAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var service = new EnrollmentService(workshopRepo.Object, enrollmentRepo.Object);

        var result = await service.EnrollParticipantAsync(new EnrollParticipantRequestDto
        {
            WorkshopId = workshopId,
            ParticipantId = participantId
        });

        Assert.Equal(EnrollmentStatus.Active, result.Status);
        Assert.Equal(EnrollmentStatus.Active, cancelledEnrollment.Status);
        Assert.Null(cancelledEnrollment.CancelledAt);
        Assert.Null(cancelledEnrollment.CancelledByUserId);
    }

    [Fact]
    public async Task GeneracionPdf_WhenEnrollmentIsNotActive_ThrowsInvalidOperationException()
    {
        var enrollmentId = Guid.NewGuid();
        var enrollment = new Enrollment
        {
            Id = enrollmentId,
            Status = EnrollmentStatus.Cancelled,
            Workshop = new Workshop
            {
                Code = "TEST-PDF",
                Title = "Taller PDF",
                StartDateTime = DateTime.UtcNow.AddDays(1),
                EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            }
        };

        var enrollmentRepo = new Mock<IEnrollmentRepository>();
        enrollmentRepo.Setup(r => r.GetByIdWithWorkshopAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        var pdfService = new QuestPdfService(enrollmentRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pdfService.GenerateEnrollmentCertificatePdfAsync(enrollmentId));
    }

    [Fact]
    public async Task CargaMasivaExcel_WhenRowsHaveValidAndInvalidData_CapturesErrorsAndCommitsValidOnes()
    {
        var participant = new ApplicationUser
        {
            Id = "user-1",
            Email = "valid@correo.com",
            FullName = "Valid User"
        };

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Code = "TEST-EXCEL",
            Title = "Taller Excel",
            StartDateTime = DateTime.UtcNow.AddDays(5),
            EndDateTime = DateTime.UtcNow.AddDays(5).AddHours(2),
            MaxCapacity = 10
        };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByEmailAsync("valid@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        userRepo.Setup(r => r.GetByEmailAsync("notfound@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var workshopRepo = new Mock<IWorkshopRepository>();
        workshopRepo.Setup(r => r.GetByCodeAsync("TEST-EXCEL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workshop);

        var enrollmentService = new Mock<IEnrollmentService>();
        enrollmentService.Setup(s => s.EnrollParticipantAsync(It.IsAny<EnrollParticipantRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnrollmentDto
            {
                Id = Guid.NewGuid(),
                WorkshopId = workshop.Id,
                ParticipantId = participant.Id,
                Status = EnrollmentStatus.Active
            });

        // Crear Excel en memoria con 2 filas (1 válida, 1 inválida)
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Enrollments");
        worksheet.Cell(1, 1).Value = "ParticipantEmail";
        worksheet.Cell(1, 2).Value = "WorkshopCode";
        worksheet.Cell(2, 1).Value = "valid@correo.com";
        worksheet.Cell(2, 2).Value = "TEST-EXCEL";
        worksheet.Cell(3, 1).Value = "notfound@correo.com";
        worksheet.Cell(3, 2).Value = "TEST-EXCEL";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var importService = new ExcelImportService(userRepo.Object, workshopRepo.Object, enrollmentService.Object);
        var result = await importService.ImportEnrollmentsFromExcelAsync(ms);

        Assert.Equal(2, result.TotalRowsProcessed);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Single(result.Errors);
        Assert.Equal(3, result.Errors[0].RowNumber);
    }
}
