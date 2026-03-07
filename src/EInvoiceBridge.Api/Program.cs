using EInvoiceBridge.Api.Middleware;
using EInvoiceBridge.Application;
using EInvoiceBridge.Delivery;
using EInvoiceBridge.Infrastructure;
using EInvoiceBridge.Infrastructure.Telemetry;
using EInvoiceBridge.Persistence;
using EInvoiceBridge.Transformation;
using EInvoiceBridge.Validation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Application layers
builder.Services.AddApplication();
builder.Services.AddValidation();
builder.Services.AddTransformation();
builder.Services.AddDelivery(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "db", "queries"));

// OpenTelemetry
builder.Services.AddOpenTelemetryDefaults("EInvoiceBridge.Api");

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

namespace EInvoiceBridge.Api
{
    public partial class Program;
}
