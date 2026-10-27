using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SkillHub.Application;
using SkillHub.Infrastructure;
using SkillHub.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Capas Application e Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Controladores
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. Configuración JWT Bearer
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SkillHubSecretKeyForEvaluationAndTesting2026!#@SkillHubDevelopmentKey";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SkillHubAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SkillHubClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Configuración Swagger con soporte JWT Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SkillHub API",
        Version = "v1",
        Description = "API RESTful para la gestión y reserva de talleres en SkillHub con Clean Architecture."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Encabezado de autorización JWT usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// 5. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 6. Migraciones y Seed Data inicial desde SkillHub_Data.xlsx
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    var excelPath = builder.Configuration["DataSeed:ExcelPath"] ?? "SkillHub_Data.xlsx";

    // Si no está en ruta relativa directa, buscar en el directorio base
    if (!File.Exists(excelPath))
    {
        var alternativePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SkillHub_Data.xlsx");
        if (File.Exists(alternativePath))
        {
            excelPath = alternativePath;
        }
        else if (File.Exists("/home/cohorte5/Descargas/SkillHub_Data.xlsx"))
        {
            excelPath = "/home/cohorte5/Descargas/SkillHub_Data.xlsx";
        }
    }

    try
    {
        await seeder.SeedAsync(excelPath);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al ejecutar el seeder de base de datos.");
    }
}

// 7. Middlewares
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillHub API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
