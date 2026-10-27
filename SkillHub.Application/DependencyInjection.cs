using Microsoft.Extensions.DependencyInjection;
using SkillHub.Application.Services.Implementations;
using SkillHub.Application.Services.Interfaces;

namespace SkillHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IWorkshopService, WorkshopService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IPdfService, QuestPdfService>();

        return services;
    }
}
