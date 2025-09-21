using System;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Domain.Services
{
    public class DomesticTransactionValidator : ITransactionValidator
    {
        public bool Validate(Transaction transaction)
        {
            return transaction.Currency == TransactionCurrency.NGN && transaction.Amount <= 100000;
        }
    }
}

