using MediatR;
using TransactionService.Application.DTOs;

namespace TransactionService.Application.Queries
{
    public record GetTransactionReceiptQuery(
        Guid TransactionId,
        string RequestedBy,
        int ExpirationHours = 24
    ) : IRequest<ReceiptDto?>;
}
