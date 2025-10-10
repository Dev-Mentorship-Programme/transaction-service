using System;

namespace TransactionService.Domain.ValueObjects
{
    public record ReceiptShareRequest
    {
        public Guid TransactionId { get; init; }
        public int ExpirationHours { get; init; }
        public string RequestedBy { get; init; }

        public ReceiptShareRequest(Guid transactionId, int expirationHours, string requestedBy)
        {
            if (transactionId == Guid.Empty)
                throw new ArgumentException("TransactionId cannot be empty", nameof(transactionId));

            if (expirationHours <= 0 || expirationHours > 168) // Max 7 days
                throw new ArgumentException("ExpirationHours must be between 1 and 168 hours", nameof(expirationHours));

            if (string.IsNullOrWhiteSpace(requestedBy))
                throw new ArgumentException("RequestedBy cannot be empty", nameof(requestedBy));

            TransactionId = transactionId;
            ExpirationHours = expirationHours;
            RequestedBy = requestedBy;
        }

        public DateTime CalculateExpirationDate()
        {
            return DateTime.UtcNow.AddHours(ExpirationHours);
        }
    }
}
