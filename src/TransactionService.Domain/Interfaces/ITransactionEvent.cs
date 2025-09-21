using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TransactionService.Domain.Interfaces
{
    public interface ITransactionEvent
    {
        string EventType { get; }
        Guid AccountId { get; }
    }
}