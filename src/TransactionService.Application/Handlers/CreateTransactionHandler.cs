using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Commands;
using TransactionService.Application.Interfaces;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Application.Handlers
{
    public class CreateTransactionCommandHandler(
        ITransactionFactory transactionFactory,
        ITransactionRepository repository,
        ILogger<CreateTransactionCommandHandler> logger) : IRequestHandler<CreateTransactionCommand, Guid>
    {
        private readonly ITransactionRepository _repository = repository;
        private readonly ITransactionFactory _transactionFactory = transactionFactory;
        private readonly ILogger<CreateTransactionCommandHandler> _logger = logger;

        public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var transactionId = request.TransactionId ?? Guid.NewGuid();
                _logger.LogInformation("Creating transaction: {TransactionId}", transactionId);

                // Check if account has a pending transaction (when TransactionId is provided from event)
                var pendingTransaction = await _repository.GetPendingAsync(request.AccountId, cancellationToken);

                if (pendingTransaction != null)
                {
                    _logger.LogInformation("Account {AccountId} has a pending transaction, returning existing ID", request.AccountId);
                    return pendingTransaction.Id;
                }

                if (request.TransactionId.HasValue)
                {
                    var existingTransaction = await _repository.GetByIdAsync(request.TransactionId.Value, cancellationToken);

                    if (existingTransaction != null)
                    {
                        _logger.LogInformation("Transaction {TransactionId} already exists, returning existing ID", request.TransactionId);
                        return existingTransaction.Id;
                    }
                }

                // Use factory to create transaction
                var transaction = _transactionFactory.Create(
                    accountId: request.AccountId,
                    destinationAccountId: request.DestinationAccountId,
                    amount: request.Amount,
                    openingBalance: request.OpeningBalance,
                    closingBalance: request.ClosingBalance ?? 0, // Factory expects non-nullable
                    narration: request.Narration,
                    type: request.Type,
                    channel: request.Channel,
                    currency: request.Currency,
                    metadata: request.Metadata
                );

                // Override the ID if provided (from event)
                if (request.TransactionId.HasValue)
                {
                    transaction.Id = request.TransactionId.Value;
                }

                await _repository.AddAsync(transaction, cancellationToken);

                _logger.LogInformation("Successfully created transaction: {TransactionId}", transaction.Id);
                return transaction.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction: {TransactionId}", request.TransactionId);
                throw;
            }
        }
    }
}