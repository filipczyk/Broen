using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Delivery.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EInvoiceBridge.Delivery;

public static class DependencyInjection
{
    public static IServiceCollection AddDelivery(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorecoveOptions>(configuration.GetSection(StorecoveOptions.SectionName));

        services.AddHttpClient<StorecoveClient>((sp, client) =>
        {
            var options = configuration.GetSection(StorecoveOptions.SectionName).Get<StorecoveOptions>()!;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });

        services.AddScoped<IDeliveryService, StorecoveDeliveryService>();

        return services;
    }
}
