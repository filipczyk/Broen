using EInvoiceBridge.Application;
using EInvoiceBridge.Delivery;
using EInvoiceBridge.Infrastructure;
using EInvoiceBridge.Persistence;
using EInvoiceBridge.Transformation;
using EInvoiceBridge.Validation;
using EInvoiceBridge.Worker.Consumers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(configuration =>
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Application layers
builder.Services.AddApplication();
builder.Services.AddValidation();
builder.Services.AddTransformation();
builder.Services.AddDelivery(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
    GetQueryBasePath());

// Consumers
builder.Services.AddHostedService<InvoiceValidationConsumer>();
builder.Services.AddHostedService<InvoiceTransformationConsumer>();
builder.Services.AddHostedService<InvoiceDeliveryConsumer>();
builder.Services.AddHostedService<InvoiceStatusConsumer>();

var host = builder.Build();
host.Run();

static string GetQueryBasePath()
{
    var dockerPath = Path.Combine(AppContext.BaseDirectory, "db", "queries");
    if (Directory.Exists(dockerPath))
        return dockerPath;

    return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "db", "queries");
}
