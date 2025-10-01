using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;

namespace TransactionService.Application.Interfaces
{
    public interface IAppDbContext
    {
        IQueryable<Transaction> Transactions { get; }
        IQueryable<SignedLink> SignedLinks { get; }
        IQueryable<ReceiptDocument> ReceiptDocuments { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    }
}