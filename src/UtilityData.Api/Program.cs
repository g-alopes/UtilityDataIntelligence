using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using UtilityData.Api.Authentication;
using UtilityData.Api.Endpoints;
using UtilityData.Application.Abstractions;
using UtilityData.Infrastructure;
using UtilityData.Infrastructure.Identity;
using UtilityData.Infrastructure.Persistence;
using UtilityData.Api.Imports;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddUtilityInfrastructure(builder.Configuration);
builder.Services.AddCors(o => o.AddPolicy("LocalWeb", p =>
    p.WithOrigins("https://localhost:7241", "http://localhost:5241")
     .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddPersistence(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) ||
    Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");

builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenant, HttpCurrentTenant>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<ImportApprovalService>();
builder.Services.Configure<ImportStorageOptions>(
    builder.Configuration.GetSection(ImportStorageOptions.SectionName));
builder.Services.AddScoped<CsvImportProcessor>();
builder.Services.AddHostedService<CsvImportWorker>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("LocalWeb");
app.UseAuthentication();
app.UseAuthorization();
await IdentityRoleSeeder.SeedAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", utc = DateTime.UtcNow }));
app.MapGet("/health/database", async (
    UtilityData.Infrastructure.Persistence.AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "available", database = "SQL Server" })
        : Results.Problem("SQL Server is not reachable.");
});
app.MapGet("/health/ai", async (IConfiguration config, CancellationToken ct) =>
{
    var endpoint = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        using var response = await client.GetAsync($"{endpoint.TrimEnd('/')}/api/tags", ct);
        return Results.Ok(new { available = response.IsSuccessStatusCode, endpoint });
    }
    catch { return Results.Ok(new { available = false, endpoint, message = "Ollama is optional/offline." }); }
});
app.MapControllers();

app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapImportEndpoints();
app.MapReadEndpoints();
app.Run();
