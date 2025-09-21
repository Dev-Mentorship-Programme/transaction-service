using System;
using TransactionService.Domain.Entities;

namespace TransactionService.Domain.Interfaces
{
    public interface ITransactionValidator
    {
        bool Validate(Transaction transaction);
    }   
}