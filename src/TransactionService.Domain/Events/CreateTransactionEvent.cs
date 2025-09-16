using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;

namespace TransactionService.Domain.Events
{
    public class CreateTransactionEvent
    {
        public Guid AccountId { get; set; }

        public Guid DestinationAccountId { get; set; }

        public decimal Amount { get; set; }

        public decimal OpeningBalance { get; set; }

        public string Narration { get; set; } = "Payment Transaction";

        public TransactionType Type { get; set; }

        public TransactionCurrency Currency { get; set; } = TransactionCurrency.NGN;

        public TransactionChannel Channel { get; set; } = TransactionChannel.TRANSFER;

        public TransactionStatus Status { get; set; } = TransactionStatus.PENDING;

        public Dictionary<string, object>? Metadata { get; set; }
    }
}