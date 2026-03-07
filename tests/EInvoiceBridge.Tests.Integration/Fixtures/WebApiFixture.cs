using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EInvoiceBridge.Tests.Integration.Fixtures;

public sealed class WebApiFixture : WebApplicationFactory<EInvoiceBridge.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Override DI registrations for integration testing
        });
    }
}
