using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransactionService.Application.Interfaces;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Data;

namespace TransactionService.Infrastructure.Repositories
{
    public class SignedLinkRepository : ISignedLinkRepository
    {
        private readonly AppDbContext _context;

        public SignedLinkRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SignedLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.SignedLinks
                .FirstOrDefaultAsync(sl => sl.Id == id, cancellationToken);
        }

        public async Task<SignedLink?> GetByUrlAsync(string shareableUrl, CancellationToken cancellationToken = default)
        {
            return await _context.SignedLinks
                .FirstOrDefaultAsync(sl => sl.ShareableUrl == shareableUrl, cancellationToken);
        }

        public async Task<List<SignedLink>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        {
            return await _context.SignedLinks
                .Where(sl => sl.TransactionId == transactionId)
                .OrderByDescending(sl => sl.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<SignedLink?> GetActiveByTransactionIdAsync(Guid transactionId, string resourceType = "Receipt", CancellationToken cancellationToken = default)
        {
            return await _context.SignedLinks
                .Where(sl => sl.TransactionId == transactionId &&
                           sl.ResourceType == resourceType &&
                           sl.IsActive &&
                           sl.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(sl => sl.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<SignedLink> AddAsync(SignedLink signedLink, CancellationToken cancellationToken = default)
        {
            _context.SignedLinks.Add(signedLink);
            await _context.SaveChangesAsync(cancellationToken);
            return signedLink;
        }

        public async Task UpdateAsync(SignedLink signedLink, CancellationToken cancellationToken = default)
        {
            _context.SignedLinks.Update(signedLink);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var signedLink = await GetByIdAsync(id, cancellationToken);
            if (signedLink != null)
            {
                _context.SignedLinks.Remove(signedLink);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.SignedLinks
                .AnyAsync(sl => sl.Id == id, cancellationToken);
        }

        public async Task<List<SignedLink>> GetExpiredLinksAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SignedLinks
                .Where(sl => sl.ExpiresAt <= DateTime.UtcNow && sl.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task DeactivateExpiredLinksAsync(CancellationToken cancellationToken = default)
        {
            var expiredLinks = await GetExpiredLinksAsync(cancellationToken);
            
            foreach (var link in expiredLinks)
            {
                link.Deactivate();
            }

            if (expiredLinks.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
