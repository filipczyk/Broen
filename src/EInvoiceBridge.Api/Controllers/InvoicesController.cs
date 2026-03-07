using EInvoiceBridge.Application.Commands.CreateInvoice;
using EInvoiceBridge.Application.Queries.GetInvoice;
using EInvoiceBridge.Application.Queries.GetInvoiceStatus;
using EInvoiceBridge.Application.Queries.GetInvoiceXml;
using EInvoiceBridge.Core.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EInvoiceBridge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateInvoiceCommand(request);
        var response = await _mediator.Send(command, cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvoiceQuery(id), cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("{id:guid}/xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "application/xml")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetXml(Guid id, CancellationToken cancellationToken)
    {
        var xml = await _mediator.Send(new GetInvoiceXmlQuery(id), cancellationToken);
        return xml is not null ? Content(xml, "application/xml") : NotFound();
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(InvoiceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvoiceStatusQuery(id), cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }
}
