using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Domain.Services
{
    public class SameAccountTransactionValidator : ITransactionValidator
    {
        public bool Validate(Transaction transaction)
        {
            return transaction.AccountId != transaction.DestinationAccountId;
        }
    }
}