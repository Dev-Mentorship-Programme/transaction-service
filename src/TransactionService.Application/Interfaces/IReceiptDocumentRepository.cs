using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;

namespace TransactionService.Application.Interfaces
{
    public interface IReceiptDocumentRepository
    {
        Task<ReceiptDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ReceiptDocument?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
        Task<List<ReceiptDocument>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ReceiptDocument> AddAsync(ReceiptDocument receiptDocument, CancellationToken cancellationToken = default);
        Task UpdateAsync(ReceiptDocument receiptDocument, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    }
}
