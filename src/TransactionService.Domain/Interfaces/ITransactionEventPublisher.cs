using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransactionService.Domain.Events;

namespace TransactionService.Domain.Interfaces
{
    public interface ITransactionEventPublisher
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task PublishAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken = default);
    }
}