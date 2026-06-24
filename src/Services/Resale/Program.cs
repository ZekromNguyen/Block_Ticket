using Shared.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSharedServices(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient("ticketing", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Ticketing"] ?? "http://localhost:5003");
});

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["IdentityServer:Authority"];
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

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

app.Run();
