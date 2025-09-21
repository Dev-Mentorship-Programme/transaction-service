using System.Collections.Generic;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Domain.Services
{
    public class CompositeTransactionValidator(IEnumerable<ITransactionValidator> validators) : ITransactionValidator
    {
        private readonly IEnumerable<ITransactionValidator> _validators = validators;

        public bool Validate(Transaction transaction)
        {
            foreach (var validator in _validators)
            {
                if (!validator.Validate(transaction))
                    return false;
            }

            return true;
        }
    }
}
