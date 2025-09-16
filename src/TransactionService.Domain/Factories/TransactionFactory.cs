using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;

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
            return new Transaction
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
        }

        public Transaction Update(
            Transaction existing,
            TransactionStatus status,
            decimal? closingBalance = null,
            string? narration = null,
            string? metadata = null
        )
        {
            if (!string.IsNullOrWhiteSpace(narration)) existing.Narration = narration;
            if (!string.IsNullOrWhiteSpace(metadata)) existing.Metadata = metadata;
            if (closingBalance.HasValue) existing.ClosingBalance = closingBalance.Value;

            existing.Status = status;
            existing.UpdatedAt = DateTime.UtcNow;

            return existing;
        }
    }
}