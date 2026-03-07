using EInvoiceBridge.Core.Interfaces;
using EInvoiceBridge.Validation.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace EInvoiceBridge.Validation;

public static class DependencyInjection
{
    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IValidationRule, SchemaCompletenessRule>();
        services.AddScoped<IValidationRule, ArithmeticRule>();
        services.AddScoped<IValidationRule, VatLogicRule>();
        services.AddScoped<IValidationRule, IdentifierFormatRule>();
        services.AddScoped<IValidationRule, GermanBusinessRule>();
        return services;
    }
}
