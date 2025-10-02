using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;

namespace TransactionService.Application.Interfaces
{
    public interface ISignedLinkRepository
    {
        Task<SignedLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<SignedLink?> GetByUrlAsync(string shareableUrl, CancellationToken cancellationToken = default);
        Task<List<SignedLink>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
        Task<SignedLink?> GetActiveByTransactionIdAsync(Guid transactionId, string resourceType = "Receipt", CancellationToken cancellationToken = default);
        Task<SignedLink> AddAsync(SignedLink signedLink, CancellationToken cancellationToken = default);
        Task UpdateAsync(SignedLink signedLink, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<SignedLink>> GetExpiredLinksAsync(CancellationToken cancellationToken = default);
        Task DeactivateExpiredLinksAsync(CancellationToken cancellationToken = default);
    }
}
