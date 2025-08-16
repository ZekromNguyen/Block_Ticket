using Event.API.Middleware;
using Event.Application.Configuration;
using Event.Infrastructure.Configuration;
using Event.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add additional configuration files
builder.Configuration.AddJsonFile("appsettings.RateLimit.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Cache.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.PerformanceMonitoring.json", optional: true, reloadOnChange: true);

// Add services to the container
builder.Services.AddControllers();

// Add API versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Version"),
        new QueryStringApiVersionReader("version"));
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Block Ticket Event API", 
        Version = "v1",
        Description = "Event Service API for Block Ticket platform"
    });
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Application layer
builder.Services.AddApplication();

// Add Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Add Health Checks
builder.Services.AddHealthChecks();

// Add Authentication & Authorization (placeholder for now)
// builder.Services.AddAuthentication("Bearer")
//     .AddJwtBearer("Bearer", options =>
//     {
//         // Configure JWT options
//     });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Block Ticket Event API v1");
        c.RoutePrefix = "swagger";
    });
}

// Add Serilog request logging
app.UseSerilogRequestLogging();

// Global exception handler
app.UseGlobalExceptionHandler();

// Performance monitoring middleware (first to capture all requests)
app.UseMiddleware<PerformanceMonitoringMiddleware>();

// Rate limiting middleware (before authentication)
app.UseMiddleware<AdvancedRateLimitMiddleware>();

// ETag middleware (before authentication and idempotency)
app.UseMiddleware<ETagMiddleware>();

// Idempotency middleware (before authentication)
app.UseMiddleware<IdempotencyMiddleware>();

// Security headers
app.UseHsts();
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAll");

// Authentication & Authorization
// app.UseAuthentication();
// app.UseAuthorization();

// Controllers
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        
        // Apply pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            Log.Information("Applying database migrations...");
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        
        // Seed data if needed
        // await SeedData(context);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while migrating or seeding the database");
        throw;
    }
}

try
{
    Log.Information("Starting Block Ticket Event API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Seed data method (placeholder)
static async Task SeedData(EventDbContext context)
{
    // Add seed data logic here if needed
    await Task.CompletedTask;
}
