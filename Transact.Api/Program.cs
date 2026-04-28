using DotNetEnv;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Transact.Application;
using Transact.Application.Transactions.Commands;
using Transact.Application.Transactions.Queries;
using Transact.Infrastructure;
using Transact.Infrastructure.Persistence;

// Load a .env file (searched in current dir and any ancestor) into process env vars
// BEFORE the WebApplicationBuilder reads configuration. Existing process env vars win
// (so docker-compose values still take precedence).
LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// If a full connection string was not supplied via configuration/env, build one from
// the discrete POSTGRES_* values that .env / docker-compose typically expose.
ApplyDatabaseConnectionString(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

const string AngularDevCors = "AngularDev";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCors, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Apply EF Core migrations on startup (with simple retry while the DB warms up).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrated.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{Max}). Retrying in 3s…", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(AngularDevCors);

app.MapGet("/", () => "Welcome to the Transact API!").WithName("Home");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).WithName("HealthCheck");

// Liveness: process is up. Used by Kubernetes livenessProbe.
app.MapGet("/health/live", () => Results.Ok(new { status = "Live" })).WithName("HealthLive");

// Readiness: dependencies (DB) are reachable. Used by Kubernetes
// readinessProbe to decide if the pod can receive traffic.
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken ct) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync(ct);
        return canConnect
            ? Results.Ok(new { status = "Ready" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).WithName("HealthReady");

app.MapPost("/transactions", async (CreateTransactionCommand command, ISender sender, CancellationToken ct) =>
{
    var transaction = await sender.Send(command, ct);
    return Results.Created($"/transactions/{transaction.Id}", transaction);
})
.WithName("CreateTransaction");

app.MapGet("/transactions", async (ISender sender, CancellationToken ct) =>
{
    var transactions = await sender.Send(new GetAllTransactionsQuery(), ct);
    return Results.Ok(transactions);
})
.WithName("GetAllTransactions");

app.MapGet("/transactions/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var transaction = await sender.Send(new GetTransactionByIdQuery(id), ct);
    return transaction is null ? Results.NotFound() : Results.Ok(transaction);
})
.WithName("GetTransactionById");

app.MapPut("/transactions/{id:guid}", async (Guid id, UpdateTransactionCommand command, ISender sender, CancellationToken ct) =>
{
    var updated = await sender.Send(command with { Id = id }, ct);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
})
.WithName("UpdateTransaction");

app.MapDelete("/transactions/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var deleted = await sender.Send(new DeleteTransactionCommand(id), ct);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteTransaction");

app.Run();

static void LoadDotEnv()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var path = Path.Combine(dir.FullName, ".env");
        if (File.Exists(path))
        {
            // Don't override variables already set in the process (e.g. from docker-compose).
            Env.Load(path, new LoadOptions(setEnvVars: true, clobberExistingVars: false));
            return;
        }
        dir = dir.Parent;
    }
}

static void ApplyDatabaseConnectionString(IConfigurationManager configuration)
{
    // Honour an explicit connection string (env var or appsettings) if present and non-empty.
    var existing = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(existing)) return;

    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
    var db = Environment.GetEnvironmentVariable("POSTGRES_DB");
    var user = Environment.GetEnvironmentVariable("POSTGRES_USER");
    var pwd = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
    var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
    var ssl = Environment.GetEnvironmentVariable("POSTGRES_SSLMODE"); // optional, e.g. Require

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(db) ||
        string.IsNullOrWhiteSpace(user) ||
        string.IsNullOrWhiteSpace(pwd))
    {
        return;
    }

    var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pwd}";
    if (!string.IsNullOrWhiteSpace(ssl))
    {
        connectionString += $";SSL Mode={ssl}";
    }

    configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}
