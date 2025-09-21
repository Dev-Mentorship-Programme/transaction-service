using TransactionService.Domain.Entities;

namespace TransactionService.Application.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Transaction>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
        Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Transaction?> GetPendingAsync(Guid accountId, CancellationToken cancellationToken = default);
    }
}