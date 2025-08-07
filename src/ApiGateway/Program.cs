using Shared.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.ConfigureSerilog();

// Add services
builder.Services.AddSharedServices();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Block Ticket API Gateway", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["IdentityServer:Authority"];
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = false;
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
    
    // Enable Swagger in all environments for API Gateway
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Block Ticket API Gateway v1");
        c.RoutePrefix = "swagger";
        
        // Add endpoints for each service (only show when services are running)
        c.SwaggerEndpoint("http://localhost:5001/swagger/v1/swagger.json", "Identity Service");
        c.SwaggerEndpoint("http://localhost:5002/swagger/v1/swagger.json", "Event Service");
        c.SwaggerEndpoint("http://localhost:5003/swagger/v1/swagger.json", "Ticketing Service");
        c.SwaggerEndpoint("http://localhost:5004/swagger/v1/swagger.json", "Resale Service");
        c.SwaggerEndpoint("http://localhost:5005/swagger/v1/swagger.json", "Verification Service");
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Map reverse proxy
app.MapReverseProxy();

// Map health checks
app.MapHealthChecks("/health");

app.Run();
