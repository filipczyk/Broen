using EInvoiceBridge.Application.Commands.ProcessWebhook;
using EInvoiceBridge.Delivery.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EInvoiceBridge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("storecove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StorecoveWebhook([FromBody] StorecoveWebhookPayload payload, CancellationToken cancellationToken)
    {
        var command = new ProcessWebhookCommand(
            payload.DocumentSubmissionId,
            payload.Status,
            payload.Timestamp);

        await _mediator.Send(command, cancellationToken);
        return Ok();
    }
}
