using Microsoft.EntityFrameworkCore;
using Shared.Common.Extensions;
using Ticketing.Application.Configuration;
using Ticketing.Infrastructure.Configuration;
using Ticketing.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.ConfigureSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add shared services
builder.Services.AddSharedServices(builder.Configuration);
builder.Services.AddTicketingApplication();
builder.Services.AddTicketingInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

// Add Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["IdentityServer:Authority"];
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("admin", "super_admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    await context.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DevSeedData.SeedAsync(context);
    }
}

// Make Program reachable for WebApplicationFactory<Program> in Ticketing.Tests.Admin.
app.Run();

public partial class Program;