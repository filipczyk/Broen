using MediatR;

namespace EInvoiceBridge.Application.Commands.ProcessWebhook;

public sealed record ProcessWebhookCommand(string SubmissionId, string Status, DateTime Timestamp) : IRequest;
