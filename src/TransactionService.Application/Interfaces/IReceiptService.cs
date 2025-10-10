using System;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;
using TransactionService.Domain.ValueObjects;

namespace TransactionService.Application.Interfaces
{
    public interface IReceiptService
    {
        Task<ReceiptDocument> GenerateReceiptAsync(Guid transactionId, CancellationToken cancellationToken = default);
        Task<SignedLink> GetShareableLinkAsync(ReceiptShareRequest request, CancellationToken cancellationToken = default);
        Task<bool> ValidateLinkAsync(string shareableUrl, CancellationToken cancellationToken = default);
    }
}
