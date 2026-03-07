using Microsoft.Extensions.Hosting;
using Serilog;

namespace EInvoiceBridge.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static IHostBuilder UseSerilogDefaults(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "EInvoiceBridge")
                .WriteTo.Console();
        });
    }
}
