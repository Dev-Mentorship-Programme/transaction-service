using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TransactionService.Domain.Interfaces
{
     public interface IEventHandler<T> where T : ITransactionEvent
    {
        Task HandleAsync(T eventData, CancellationToken cancellationToken);
    }
}