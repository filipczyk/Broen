using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace EInvoiceBridge.Tests.Architecture;

public class LayerDependencyTests
{
    private const string CoreNamespace = "EInvoiceBridge.Core";
    private const string ApplicationNamespace = "EInvoiceBridge.Application";
    private const string ValidationNamespace = "EInvoiceBridge.Validation";
    private const string TransformationNamespace = "EInvoiceBridge.Transformation";
    private const string DeliveryNamespace = "EInvoiceBridge.Delivery";
    private const string PersistenceNamespace = "EInvoiceBridge.Persistence";
    private const string InfrastructureNamespace = "EInvoiceBridge.Infrastructure";
    private const string ApiNamespace = "EInvoiceBridge.Api";
    private const string WorkerNamespace = "EInvoiceBridge.Worker";

    [Fact]
    public void Core_ShouldNotDependOnAnyOtherProject()
    {
        var result = Types
            .InAssembly(typeof(EInvoiceBridge.Core.Models.Invoice).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                ValidationNamespace,
                TransformationNamespace,
                DeliveryNamespace,
                PersistenceNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldOnlyDependOnCore()
    {
        var result = Types
            .InAssembly(typeof(EInvoiceBridge.Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ValidationNamespace,
                TransformationNamespace,
                DeliveryNamespace,
                PersistenceNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Validation_ShouldOnlyDependOnCore()
    {
        var result = Types
            .InAssembly(typeof(EInvoiceBridge.Validation.ValidationService).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                TransformationNamespace,
                DeliveryNamespace,
                PersistenceNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Transformation_ShouldOnlyDependOnCore()
    {
        var result = Types
            .InAssembly(typeof(EInvoiceBridge.Transformation.UblInvoiceTransformer).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                ValidationNamespace,
                DeliveryNamespace,
                PersistenceNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Delivery_ShouldOnlyDependOnCore()
    {
        var result = Types
            .InAssembly(typeof(EInvoiceBridge.Delivery.StorecoveDeliveryService).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                ValidationNamespace,
                TransformationNamespace,
                PersistenceNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Persistence_ShouldOnlyDependOnCore()
    {
        var result = Types
            .InAssembly(typeof(EInvoiceBridge.Persistence.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                ValidationNamespace,
                TransformationNamespace,
                DeliveryNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldOnlyDependOnCore()
    {
        var result = Types
            .InAssembly(typeof(EInvoiceBridge.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                ValidationNamespace,
                TransformationNamespace,
                DeliveryNamespace,
                PersistenceNamespace,
                ApiNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
