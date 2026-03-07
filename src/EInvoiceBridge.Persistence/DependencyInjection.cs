using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Persistence.Connection;
using EInvoiceBridge.Persistence.Queries;
using EInvoiceBridge.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EInvoiceBridge.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString, string queryBasePath)
    {
        services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));
        services.AddSingleton<IQueryLoader>(new EmbeddedQueryLoader(queryBasePath));
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IFormatRepository, FormatRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        return services;
    }
}
