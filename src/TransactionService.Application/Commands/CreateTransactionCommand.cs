using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TransactionService.Domain.Entities;

namespace TransactionService.Application.Commands
{
    public class CreateTransactionCommand : IRequest<Guid>
    {
        public Guid? TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal Amount { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public string Narration { get; set; } = "Payment Transaction";
        public TransactionType Type { get; set; }
        public TransactionCurrency Currency { get; set; } = TransactionCurrency.NGN;
        public TransactionChannel Channel { get; set; } = TransactionChannel.TRANSFER;
        public string? Metadata { get; set; }
    }
}