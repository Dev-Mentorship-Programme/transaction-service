using Microsoft.Extensions.Logging;
using MediatR;
using TransactionService.Domain.Interfaces;
using TransactionService.Application.Events;

namespace TransactionService.Application.Handlers
{
    public class TransactionCreatedHandler(
        ITransactionEventPublisherFactory publisherFactory,
        ILogger<TransactionCreatedHandler> logger) : INotificationHandler<TransactionCreatedNotification>
{
    private readonly ITransactionEventPublisherFactory _publisherFactory = publisherFactory;
    private readonly ILogger<TransactionCreatedHandler> _logger = logger;

    public async Task Handle(TransactionCreatedNotification notification, CancellationToken cancellationToken)
    {
        var publisher = await _publisherFactory.CreateAsync();
        _logger.LogInformation("Publishing TransactionCreatedNotification, {Event}", notification.Event.TransactionId);
        await publisher.PublishAsync(notification.Event, cancellationToken);
    }
}
}