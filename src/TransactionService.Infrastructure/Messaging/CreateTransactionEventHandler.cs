using TransactionService.Domain.Events;
using TransactionService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TransactionService.Application.Commands;
using TransactionService.Application.Events;

namespace TransactionService.Infrastructure.Messaging
{
    public class CreateTransactionEventHandler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CreateTransactionEventHandler> logger) : IEventHandler<CreateTransactionEvent>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<CreateTransactionEventHandler> _logger = logger;

        public async Task HandleAsync(CreateTransactionEvent transactionEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing CreateTransactionEvent for AccountId: {AccountId}", transactionEvent.AccountId);

            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Create and send command
            var command = new CreateTransactionCommand
            {
                AccountId = transactionEvent.AccountId,
                DestinationAccountId = transactionEvent.DestinationAccountId,
                Amount = transactionEvent.Amount,
                OpeningBalance = transactionEvent.OpeningBalance,
                Narration = transactionEvent.Narration,
                Type = transactionEvent.Type,
                Currency = transactionEvent.Currency,
                Channel = transactionEvent.Channel,
                Metadata = JsonSerializer.Serialize(transactionEvent.Metadata),
            };

            var transactionId = await mediator.Send(command, cancellationToken);

            // Create and publish notification
            var createdEvent = new TransactionCreatedEvent(transactionId);
            var notification = new TransactionCreatedNotification(createdEvent);

            await mediator.Publish(notification, cancellationToken);

            _logger.LogInformation("Successfully processed CreateTransactionEvent for AccountId: {AccountId}", transactionEvent.AccountId);
        }
    }
}