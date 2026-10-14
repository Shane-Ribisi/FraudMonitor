using System.Text.Json.Serialization;
using FraudMonitor.Data;
using FraudMonitor.Models;
using FraudMonitor.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/fraud-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<FraudRuleEngine>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddHostedService<TransactionSimulator>();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();

// ---- Transactions ----
app.MapPost("/api/transactions", async (TransactionRequest req, TransactionService svc) =>
{
    var result = await svc.ProcessAsync(req);
    return result is null ? Results.NotFound("Account not found") : Results.Ok(result);
});

app.MapGet("/api/transactions", async (AppDbContext db, int take = 25) =>
    await db.Transactions
        .OrderByDescending(t => t.Timestamp)
        .Take(Math.Clamp(take, 1, 200))
        .Select(t => new
        {
            t.Id, AccountNumber = t.Account!.AccountNumber, t.Amount, t.Merchant,
            t.Country, t.Timestamp, t.Status, t.RiskScore, t.ProcessingMs
        })
        .ToListAsync());

// ---- Accounts ----
app.MapGet("/api/accounts", async (AppDbContext db) =>
    await db.Accounts
        .OrderBy(a => a.AccountNumber)
        .Select(a => new { a.AccountNumber, a.HolderName, a.Balance, a.HomeCountry })
        .ToListAsync());

// ---- Alerts ----
app.MapGet("/api/alerts", async (AppDbContext db, bool unresolved = false, int take = 25) =>
    await db.Alerts
        .Where(a => !unresolved || !a.Resolved)
        .OrderByDescending(a => a.CreatedAt)
        .Take(Math.Clamp(take, 1, 200))
        .Select(a => new
        {
            a.Id, a.TransactionId, a.Rule, a.Severity, a.Description,
            a.CreatedAt, a.Resolved, a.ResolvedAt
        })
        .ToListAsync());

app.MapPost("/api/alerts/{id:int}/resolve", async (int id, AppDbContext db, AuditService audit) =>
{
    var alert = await db.Alerts.FindAsync(id);
    if (alert is null) return Results.NotFound();
    if (alert.Resolved) return Results.Ok(alert.Id);
    alert.Resolved = true;
    alert.ResolvedAt = DateTime.UtcNow;
    audit.Record("ALERT_RESOLVED", "FraudAlert", id.ToString(), $"Rule {alert.Rule} resolved by analyst");
    await db.SaveChangesAsync();
    return Results.Ok(alert.Id);
});

// ---- Audit & stats ----
app.MapGet("/api/audit", async (AppDbContext db, int take = 50) =>
    await db.AuditLogs.OrderByDescending(a => a.Id).Take(Math.Clamp(take, 1, 500)).ToListAsync());

app.MapGet("/api/stats", async (AppDbContext db) =>
{
    var total = await db.Transactions.CountAsync();
    var settled = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Settled);
    var flagged = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Flagged);
    var blocked = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Blocked);
    var declined = await db.Transactions.CountAsync(t => t.Status == TransactionStatus.Declined);
    var openAlerts = await db.Alerts.CountAsync(a => !a.Resolved);
    var avgMs = total == 0 ? 0 : await db.Transactions.Select(t => (double?)t.ProcessingMs).AverageAsync() ?? 0;
    return new { total, settled, flagged, blocked, declined, openAlerts, avgProcessingMs = Math.Round(avgMs, 2) };
});

try { app.Run(); }
finally { Log.CloseAndFlush(); }
