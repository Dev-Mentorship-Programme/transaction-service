using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;

namespace TransactionService.Domain.Interfaces
{
    public interface ITransactionFactory
    {
        Transaction Create(
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
        );

        Transaction Update(
            Transaction transaction,
            TransactionStatus status,
            decimal? closingBalance,
            string? narration,
            string? metadata
        );
    }
}