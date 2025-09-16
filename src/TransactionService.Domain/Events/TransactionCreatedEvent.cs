using System;

namespace TransactionService.Domain.Events
{
   public class TransactionCreatedEvent(Guid transactionId)
    {
        public Guid TransactionId { get; } = transactionId;
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}
