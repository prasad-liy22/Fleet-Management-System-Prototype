using FleetManagement.Infrastructure;
using FleetManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Seed:Enabled") || args.Contains("--seed-only"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync();
    if (args.Contains("--seed-only"))
        return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Frontend");
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
public partial class Program { }
