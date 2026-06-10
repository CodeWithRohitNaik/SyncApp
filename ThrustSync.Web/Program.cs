using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Console;
using Azure.Identity;
using ThrustSync.Data.Context;
using ThrustSync.Data.Repositories;
using ThrustSync.Core.Services;
using ThrustSync.Core.Models;
using ThrustSync.Core.Repositories;
using ThrustSync.ETL.Services;
using ThrustSync.ETL.Jobs;
using ThrustSync.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURATION ====================
var environment = builder.Environment.EnvironmentName;
var isDevelopment = builder.Environment.IsDevelopment();

// Add configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ==================== KEY VAULT INTEGRATION ====================
if (!isDevelopment)
{
    // Key Vault configuration would go here if needed
    // var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    // if (!string.IsNullOrEmpty(keyVaultUrl))
    // {
    //     builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
    // }
}

// ==================== DATABASE CONFIGURATION ====================
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<TrustSyncDbContext>(options =>
    options.UseSqlServer(sqlConnectionString,
        sqlOptions => sqlOptions
            .MigrationsAssembly("ThrustSync.Data")
            .EnableRetryOnFailure(maxRetryCount: 5))
);

// ==================== APPLICATION INSIGHTS ====================
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("ApplicationInsights");
    options.DeveloperMode = isDevelopment;
});

// ==================== DEPENDENCY INJECTION ====================
// Repositories
builder.Services.AddScoped<IRepository<AuditLog>, Repository<AuditLog>>();
builder.Services.AddScoped<IRepository<ExportLog>, Repository<ExportLog>>();
builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
builder.Services.AddScoped<IApuRepository, ApuRepository>();
builder.Services.AddScoped<IKpiEntryRepository, KpiEntryRepository>();

// Core Services
builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// ETL Services
builder.Services.AddScoped<IOracleService, OracleService>();
builder.Services.AddScoped<OracleEtlJob>();

// Web Services
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

// ==================== HANGFIRE BACKGROUND JOBS ====================

builder.Services.AddHangfire(config =>
{
    config.UseSqlServerStorage(sqlConnectionString)
        .WithJobExpirationTimeout(TimeSpan.FromDays(7))
        .UseConsole();
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount;
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// ==================== MVC AND API CONFIGURATION ====================
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// Enable CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ==================== LOGGING ====================
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
    config.AddApplicationInsights();
    
    if (isDevelopment)
    {
        config.SetMinimumLevel(LogLevel.Debug);
    }
});

// ==================== BUILD APP ====================
var app = builder.Build();

// ==================== MIDDLEWARE PIPELINE ====================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");

// ==================== HANGFIRE DASHBOARD ====================
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// ==================== ENDPOINT MAPPING ====================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.MapRazorPages();

// ==================== DATABASE MIGRATIONS ====================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TrustSyncDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        throw;
    }
}

// ==================== SCHEDULE HANGFIRE JOBS ====================
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Scheduling Hangfire jobs...");

        // Schedule daily Oracle ETL at 2 AM UTC
        recurringJobManager.AddOrUpdate<OracleEtlJob>(
            "oracle-etl-daily",
            job => job.ExecuteAsync(null),
            "0 2 * * *", // Cron expression: 2 AM daily
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
        );

        logger.LogInformation("Hangfire jobs scheduled successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while scheduling Hangfire jobs");
    }
}
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application started in {Environment} environment", environment);
}
app.Run();

// ==================== HANGFIRE AUTHORIZATION FILTER ====================
/// <summary>
/// Simple authorization filter for Hangfire dashboard
/// In production, implement proper authentication/authorization
/// </summary>
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // In development, allow all access
        // In production, check user roles/claims
        return true;
    }
}
