using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Data;
using TransactionService.Infrastructure.Repositories;
using Xunit;

namespace TransactionService.Infrastructure.Tests.Repositories
{
    public class SignedLinkRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SignedLinkRepository _repository;

        public SignedLinkRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repository = new SignedLinkRepository(_context);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnSignedLink()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var signedLink = new SignedLink(transactionId, "https://example.com/receipt/123", DateTime.UtcNow.AddHours(24));
            
            _context.SignedLinks.Add(signedLink);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(signedLink.Id);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(signedLink.Id, result.Id);
                Assert.Equal(transactionId, result.TransactionId);
            });
        }

        [Fact]
        public async Task GetByUrlAsync_WithValidUrl_ShouldReturnSignedLink()
        {
            // Arrange
            var shareableUrl = "https://example.com/receipt/unique123";
            var signedLink = new SignedLink(Guid.NewGuid(), shareableUrl, DateTime.UtcNow.AddHours(24));
            
            _context.SignedLinks.Add(signedLink);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUrlAsync(shareableUrl);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(shareableUrl, result.ShareableUrl);
            });
        }

        [Fact]
        public async Task GetByTransactionIdAsync_WithValidId_ShouldReturnAllLinksForTransaction()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var otherTransactionId = Guid.NewGuid();
            
            var link1 = new SignedLink(transactionId, "https://example.com/receipt/1", DateTime.UtcNow.AddHours(24));
            var link2 = new SignedLink(transactionId, "https://example.com/receipt/2", DateTime.UtcNow.AddHours(48));
            var link3 = new SignedLink(otherTransactionId, "https://example.com/receipt/3", DateTime.UtcNow.AddHours(24));

            _context.SignedLinks.AddRange(link1, link2, link3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByTransactionIdAsync(transactionId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(2, result.Count());
                Assert.True(result.All(sl => sl.TransactionId == transactionId));
            });
        }

        [Fact]
        public async Task GetActiveByTransactionIdAsync_WithActiveLink_ShouldReturnActiveLink()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var activeLink = new SignedLink(transactionId, "https://example.com/receipt/active", DateTime.UtcNow.AddHours(24));
            var expiredLink = new SignedLink
            {
                TransactionId = transactionId,
                ShareableUrl = "https://example.com/receipt/expired",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                ResourceType = "Receipt",
                IsActive = true
            };

            _context.SignedLinks.AddRange(activeLink, expiredLink);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveByTransactionIdAsync(transactionId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(activeLink.Id, result.Id);
                Assert.True(result.IsValid);
            });
        }

        [Fact]
        public async Task AddAsync_WithValidSignedLink_ShouldAddToDatabase()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var signedLink = new SignedLink(transactionId, "https://example.com/receipt/new", DateTime.UtcNow.AddHours(24));

            // Act
            var result = await _repository.AddAsync(signedLink);

            // Assert
            Assert.Equal(signedLink, result);
            
            var saved = await _context.SignedLinks.FindAsync(signedLink.Id);
            Assert.Multiple(() =>
            {
                Assert.NotNull(saved);
                Assert.Equal(transactionId, saved.TransactionId);
            });
        }

        [Fact]
        public async Task UpdateAsync_WithExistingSignedLink_ShouldUpdateDatabase()
        {
            // Arrange
            var signedLink = new SignedLink(Guid.NewGuid(), "https://example.com/receipt/update", DateTime.UtcNow.AddHours(24));
            _context.SignedLinks.Add(signedLink);
            await _context.SaveChangesAsync();

            // Act
            signedLink.Deactivate();
            await _repository.UpdateAsync(signedLink);

            // Assert
            var updated = await _context.SignedLinks.FindAsync(signedLink.Id);
            Assert.Multiple(() =>
            {
                Assert.NotNull(updated);
                Assert.False(updated.IsActive);
            });
        }

        [Fact]
        public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            var signedLink = new SignedLink(Guid.NewGuid(), "https://example.com/receipt/exists", DateTime.UtcNow.AddHours(24));
            _context.SignedLinks.Add(signedLink);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(signedLink.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetExpiredLinksAsync_ShouldReturnOnlyExpiredActiveLinks()
        {
            // Arrange
            var activeValidLink = new SignedLink(Guid.NewGuid(), "https://example.com/receipt/valid", DateTime.UtcNow.AddHours(24));
            var expiredActiveLink = new SignedLink
            {
                TransactionId = Guid.NewGuid(),
                ShareableUrl = "https://example.com/receipt/expired-active",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                ResourceType = "Receipt",
                IsActive = true
            };
            var expiredInactiveLink = new SignedLink
            {
                TransactionId = Guid.NewGuid(),
                ShareableUrl = "https://example.com/receipt/expired-inactive",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                ResourceType = "Receipt",
                IsActive = false
            };

            _context.SignedLinks.AddRange(activeValidLink, expiredActiveLink, expiredInactiveLink);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetExpiredLinksAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Single(result);
                Assert.Equal(expiredActiveLink.Id, result.First().Id);
            });
        }

        [Fact]
        public async Task DeactivateExpiredLinksAsync_ShouldDeactivateExpiredActiveLinks()
        {
            // Arrange
            var expiredActiveLink = new SignedLink
            {
                TransactionId = Guid.NewGuid(),
                ShareableUrl = "https://example.com/receipt/expired",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                ResourceType = "Receipt",
                IsActive = true
            };

            _context.SignedLinks.Add(expiredActiveLink);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeactivateExpiredLinksAsync();

            // Assert
            var updated = await _context.SignedLinks.FindAsync(expiredActiveLink.Id);
            Assert.Multiple(() =>
            {
                Assert.NotNull(updated);
                Assert.False(updated.IsActive);
            });
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
