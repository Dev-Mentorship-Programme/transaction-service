using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransactionService.Application.Interfaces;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Data;

namespace TransactionService.Infrastructure.Repositories
{
    public class ReceiptDocumentRepository : IReceiptDocumentRepository
    {
        private readonly AppDbContext _context;

        public ReceiptDocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReceiptDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ReceiptDocuments
                .FirstOrDefaultAsync(rd => rd.Id == id, cancellationToken);
        }

        public async Task<ReceiptDocument?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        {
            return await _context.ReceiptDocuments
                .FirstOrDefaultAsync(rd => rd.TransactionId == transactionId, cancellationToken);
        }

        public async Task<List<ReceiptDocument>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ReceiptDocuments
                .OrderByDescending(rd => rd.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ReceiptDocument> AddAsync(ReceiptDocument receiptDocument, CancellationToken cancellationToken = default)
        {
            _context.ReceiptDocuments.Add(receiptDocument);
            await _context.SaveChangesAsync(cancellationToken);
            return receiptDocument;
        }

        public async Task UpdateAsync(ReceiptDocument receiptDocument, CancellationToken cancellationToken = default)
        {
            _context.ReceiptDocuments.Update(receiptDocument);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var receiptDocument = await GetByIdAsync(id, cancellationToken);
            if (receiptDocument != null)
            {
                _context.ReceiptDocuments.Remove(receiptDocument);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ReceiptDocuments
                .AnyAsync(rd => rd.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        {
            return await _context.ReceiptDocuments
                .AnyAsync(rd => rd.TransactionId == transactionId, cancellationToken);
        }
    }
}
