using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkillHub.Application.Common.Interfaces;
using SkillHub.Application.Services.Interfaces;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Application.Services.Implementations;

public class QuestPdfService : IPdfService
{
    private readonly IEnrollmentRepository _enrollmentRepository;

    public QuestPdfService(IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateEnrollmentCertificatePdfAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdWithWorkshopAsync(enrollmentId, cancellationToken);
        if (enrollment == null)
        {
            throw new DomainException($"No se encontró la inscripción con ID {enrollmentId}.");
        }

        // Regla: Solo para estado Active
        if (enrollment.Status != EnrollmentStatus.Active)
        {
            throw new InvalidOperationException("Solo se permite generar el comprobante en formato PDF para inscripciones en estado Active.");
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("SkillHub").Bold().FontSize(22).FontColor(Colors.Blue.Medium);
                            col.Item().Text("Comprobante Oficial de Inscripción").FontSize(14).FontColor(Colors.Grey.Darken2);
                        });
                    });

                page.Content()
                    .PaddingVertical(1.5f, Unit.Centimetre)
                    .Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text(text =>
                        {
                            text.Span("Comprobante N°: ").Bold();
                            text.Span(enrollment.Id.ToString());
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Estado: ").Bold();
                            text.Span(enrollment.Status.ToString()).FontColor(Colors.Green.Medium).Bold();
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Fecha de Inscripción: ").Bold();
                            text.Span(enrollment.EnrolledAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                        });

                        col.Item().PaddingTop(10).Text("Datos del Taller").Bold().FontSize(14).FontColor(Colors.Blue.Medium);

                        col.Item().Text(text =>
                        {
                            text.Span("Código del Taller: ").Bold();
                            text.Span(enrollment.Workshop.Code);
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Título: ").Bold();
                            text.Span(enrollment.Workshop.Title);
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Descripción: ").Bold();
                            text.Span(enrollment.Workshop.Description);
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Fecha y Hora de Inicio: ").Bold();
                            text.Span(enrollment.Workshop.StartDateTime.ToString("yyyy-MM-dd HH:mm UTC"));
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Fecha y Hora de Fin: ").Bold();
                            text.Span(enrollment.Workshop.EndDateTime.ToString("yyyy-MM-dd HH:mm UTC"));
                        });

                        col.Item().PaddingTop(10).Text("Datos del Participante").Bold().FontSize(14).FontColor(Colors.Blue.Medium);

                        col.Item().Text(text =>
                        {
                            text.Span("ID Participante: ").Bold();
                            text.Span(enrollment.ParticipantId);
                        });

                        if (!string.IsNullOrWhiteSpace(enrollment.Participant?.FullName))
                        {
                            col.Item().Text(text =>
                            {
                                text.Span("Nombre del Participante: ").Bold();
                                text.Span(enrollment.Participant.FullName);
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generado automáticamente por SkillHub - ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}
