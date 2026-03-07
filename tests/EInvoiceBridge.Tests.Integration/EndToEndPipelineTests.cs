using EInvoiceBridge.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace EInvoiceBridge.Tests.Integration;

public class EndToEndPipelineTests : IClassFixture<WebApiFixture>
{
    private readonly WebApiFixture _fixture;
    private readonly HttpClient _client;

    public EndToEndPipelineTests(WebApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact(Skip = "Stub — requires full infrastructure")]
    public async Task CreateInvoice_EndToEnd_ReturnsAccepted()
    {
        // TODO: POST invoice, verify 202, check status progression
    }
}
