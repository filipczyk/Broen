using System.Net;
using System.Text;
using System.Text.Json;
using EInvoiceBridge.Delivery;
using EInvoiceBridge.Delivery.Models;
using EInvoiceBridge.Delivery.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace EInvoiceBridge.Tests.Unit.Delivery;

public class StorecoveDeliveryServiceTests
{
    private const long TestLegalEntityId = 42;
    private readonly StorecoveOptions _options = new() { LegalEntityId = TestLegalEntityId, BaseUrl = "https://api.storecove.com/api/v2" };

    private (StorecoveDeliveryService service, MockHttpHandler handler) CreateService(string responseJson = """{"guid":"resp-guid-123"}""", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var mockHandler = new MockHttpHandler(responseJson, statusCode);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri(_options.BaseUrl) };
        var client = new StorecoveClient(httpClient, Options.Create(_options));
        var service = new StorecoveDeliveryService(client, Options.Create(_options));
        return (service, mockHandler);
    }

    [Fact]
    public async Task SubmitAsync_EncodesXmlAsBase64()
    {
        var (service, handler) = CreateService();
        var xml = "<Invoice>test</Invoice>";

        await service.SubmitAsync(Guid.NewGuid(), xml, "DE123456789");

        var requestBody = handler.CapturedRequestBody;
        var request = JsonSerializer.Deserialize<StorecoveSubmissionRequest>(requestBody);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request!.Document.RawDocumentData.Document));
        decoded.Should().Be(xml);
    }

    [Fact]
    public async Task SubmitAsync_SetsLegalEntityId()
    {
        var (service, handler) = CreateService();

        await service.SubmitAsync(Guid.NewGuid(), "<Invoice/>", "DE123456789");

        var request = JsonSerializer.Deserialize<StorecoveSubmissionRequest>(handler.CapturedRequestBody);
        request!.LegalEntityId.Should().Be(TestLegalEntityId);
    }

    [Fact]
    public async Task SubmitAsync_RoutesGermanBuyerWithDeVatScheme()
    {
        var (service, handler) = CreateService();

        await service.SubmitAsync(Guid.NewGuid(), "<Invoice/>", "DE123456789");

        var request = JsonSerializer.Deserialize<StorecoveSubmissionRequest>(handler.CapturedRequestBody);
        request!.Routing.EIdentifiers.Should().ContainSingle()
            .Which.Scheme.Should().Be("DE:VAT");
    }

    [Fact]
    public async Task SubmitAsync_RoutesBelgianBuyerWithBeEnScheme()
    {
        var (service, handler) = CreateService();

        await service.SubmitAsync(Guid.NewGuid(), "<Invoice/>", "BE0123456789");

        var request = JsonSerializer.Deserialize<StorecoveSubmissionRequest>(handler.CapturedRequestBody);
        request!.Routing.EIdentifiers.Should().ContainSingle()
            .Which.Scheme.Should().Be("BE:EN");
    }

    [Fact]
    public async Task SubmitAsync_HttpError_Throws()
    {
        var (service, _) = CreateService("{}", HttpStatusCode.InternalServerError);

        var act = () => service.SubmitAsync(Guid.NewGuid(), "<Invoice/>", "DE123456789");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task SubmitAsync_ReturnsGuidFromResponse()
    {
        var (service, _) = CreateService("""{"guid":"my-guid-456"}""");

        var result = await service.SubmitAsync(Guid.NewGuid(), "<Invoice/>", "DE123456789");

        result.Should().Be("my-guid-456");
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly string _responseJson;
        private readonly HttpStatusCode _statusCode;
        public string CapturedRequestBody { get; private set; } = string.Empty;

        public MockHttpHandler(string responseJson, HttpStatusCode statusCode)
        {
            _responseJson = responseJson;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
