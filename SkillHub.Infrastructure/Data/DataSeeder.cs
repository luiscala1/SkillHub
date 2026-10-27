using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;

namespace SkillHub.Infrastructure.Data;

public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync(string excelPath)
    {
        await _context.Database.EnsureCreatedAsync();

        // 1. Roles
        string[] roles = { "Employee", "Participant" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
                _logger.LogInformation("Rol creado: {Role}", role);
            }
        }

        if (!File.Exists(excelPath))
        {
            _logger.LogWarning("El archivo Excel para el seeder no fue encontrado en: {Path}", excelPath);
            return;
        }

        using var workbook = new XLWorkbook(excelPath);

        // 2. Instructors (Role: Employee)
        if (workbook.TryGetWorksheet("Instructors", out var instructorsSheet))
        {
            var rows = instructorsSheet.RowsUsed().Skip(1);
            foreach (var row in rows)
            {
                var fullName = row.Cell(1).GetString().Trim();
                var email = row.Cell(2).GetString().Trim();
                var password = row.Cell(3).GetString().Trim();
                var specialty = row.Cell(4).GetString().Trim();

                if (await _userManager.FindByEmailAsync(email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = fullName,
                        Specialty = specialty,
                        Role = "Employee",
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Employee");
                        _logger.LogInformation("Instructor creado: {Email}", email);
                    }
                    else
                    {
                        _logger.LogError("Error al crear instructor {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        // 3. Participants (Role: Participant)
        if (workbook.TryGetWorksheet("Participants", out var participantsSheet))
        {
            var rows = participantsSheet.RowsUsed().Skip(1);
            foreach (var row in rows)
            {
                var fullName = row.Cell(1).GetString().Trim();
                var email = row.Cell(2).GetString().Trim();
                var password = row.Cell(3).GetString().Trim();

                if (await _userManager.FindByEmailAsync(email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = fullName,
                        Specialty = null,
                        Role = "Participant",
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Participant");
                        _logger.LogInformation("Participante creado: {Email}", email);
                    }
                    else
                    {
                        _logger.LogError("Error al crear participante {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        // 4. Workshops
        if (workbook.TryGetWorksheet("Workshops", out var workshopsSheet))
        {
            var rows = workshopsSheet.RowsUsed().Skip(1);
            foreach (var row in rows)
            {
                var code = row.Cell(1).GetString().Trim();
                var title = row.Cell(2).GetString().Trim();
                var description = row.Cell(3).GetString().Trim();
                var instructorEmail = row.Cell(4).GetString().Trim();
                var startDateTime = row.Cell(5).GetDateTime();
                var endDateTime = row.Cell(6).GetDateTime();
                var capacity = row.Cell(7).GetValue<int>();

                if (!await _context.Workshops.AnyAsync(w => w.Code == code))
                {
                    var instructor = await _userManager.FindByEmailAsync(instructorEmail);
                    if (instructor != null)
                    {
                        var workshop = new Workshop
                        {
                            Id = Guid.NewGuid(),
                            Code = code,
                            Title = title,
                            Description = description,
                            InstructorId = instructor.Id,
                            StartDateTime = DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc),
                            EndDateTime = DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc),
                            MaxCapacity = capacity
                        };

                        await _context.Workshops.AddAsync(workshop);
                        _logger.LogInformation("Taller creado: {Code} - {Title}", code, title);
                    }
                }
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("Talleres cargados exitosamente desde Excel.");
        }

        _logger.LogInformation("Seeder finalizado con éxito (Instructores, Participantes y Talleres cargados). La hoja Enrollments se reserva para el endpoint de importación.");
    }
}
