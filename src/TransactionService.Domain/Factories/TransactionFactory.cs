using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;
using TransactionService.Domain.Services;

namespace TransactionService.Domain.Factories
{
    public class TransactionFactory : ITransactionFactory
    {
        public Transaction Create(
            Guid accountId,
            Guid destinationAccountId,
            decimal amount,
            decimal openingBalance,
            decimal closingBalance,
            string narration,
            TransactionType type,
            TransactionChannel channel,
            TransactionCurrency currency,
            string? reference = null,
            string? metadata = null
        )
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
            if (closingBalance > openingBalance && type == TransactionType.DEBIT)
                throw new InvalidOperationException("Closing balance cannot exceed opening balance for debit transactions.");

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                DestinationAccountId = destinationAccountId,
                Amount = amount,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                Narration = narration,
                Type = type,
                Currency = currency,
                Channel = channel,
                Status = TransactionStatus.PENDING,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Reference = reference ?? $"TR-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                Metadata = metadata
            };
            var validators = new List<ITransactionValidator>
            {
                new SameAccountTransactionValidator()
            };
            var sameAccountTransaction = new CompositeTransactionValidator(validators);
            var result = sameAccountTransaction.Validate(transaction);
            if (!result)
                throw new InvalidOperationException("Sender and recipient accounts can't be the same.");

            return transaction;
        }

        public Transaction Update(
            Transaction existing,
            TransactionStatus status,
            decimal? closingBalance = null,
            string? narration = null,
            string? metadata = null
        )
        {
            if (existing.Type == TransactionType.DEBIT && closingBalance > existing.OpeningBalance)
                throw new InvalidOperationException("Closing balance cannot exceed opening balance for debit transactions.");
            if (existing.Type == TransactionType.CREDIT && closingBalance < existing.OpeningBalance)
                throw new InvalidOperationException("Closing balance cannot be less than opening balance for credit transactions.");
            if (!Enum.IsDefined(status))
                throw new ArgumentException("Invalid transaction status.", nameof(status));

            if (!string.IsNullOrWhiteSpace(narration)) existing.Narration = narration;
            if (!string.IsNullOrWhiteSpace(metadata)) existing.Metadata = metadata;
            if (closingBalance.HasValue) existing.ClosingBalance = closingBalance.Value;

            existing.Status = status;
            existing.UpdatedAt = DateTime.UtcNow;

            return existing;
        }
    }
}