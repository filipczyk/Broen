using EInvoiceBridge.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EInvoiceBridge.Transformation;

public static class DependencyInjection
{
    public static IServiceCollection AddTransformation(this IServiceCollection services)
    {
        services.AddScoped<ITransformationService, UblInvoiceTransformer>();
        services.AddSingleton<XsdValidator>();
        return services;
    }
}
