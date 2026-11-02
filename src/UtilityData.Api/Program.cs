using UtilityData.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddUtilityInfrastructure(builder.Configuration);
builder.Services.AddCors(o => o.AddPolicy("LocalWeb", p =>
    p.WithOrigins("https://localhost:7241", "http://localhost:5241")
     .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddPersistence(builder.Configuration);
var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors("LocalWeb");
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
app.Run();
