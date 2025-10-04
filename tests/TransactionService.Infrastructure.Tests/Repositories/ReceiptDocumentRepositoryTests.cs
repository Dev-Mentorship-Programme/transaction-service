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
    public class ReceiptDocumentRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ReceiptDocumentRepository _repository;

        public ReceiptDocumentRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repository = new ReceiptDocumentRepository(_context);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnReceiptDocument()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var receiptDocument = new ReceiptDocument(transactionId, "https://cloudinary.com/receipt.pdf", "receipt123");
            
            _context.ReceiptDocuments.Add(receiptDocument);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(receiptDocument.Id);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(receiptDocument.Id, result.Id);
                Assert.Equal(transactionId, result.TransactionId);
            });
        }

        [Fact]
        public async Task GetByTransactionIdAsync_WithValidTransactionId_ShouldReturnReceiptDocument()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var receiptDocument = new ReceiptDocument(transactionId, "https://cloudinary.com/receipt.pdf", "receipt123");
            
            _context.ReceiptDocuments.Add(receiptDocument);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByTransactionIdAsync(transactionId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(transactionId, result.TransactionId);
                Assert.Equal("https://cloudinary.com/receipt.pdf", result.DocumentUrl);
            });
        }

        [Fact]
        public async Task GetByTransactionIdAsync_WithNonExistentTransactionId_ShouldReturnNull()
        {
            // Arrange
            var nonExistentTransactionId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByTransactionIdAsync(nonExistentTransactionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_WithMultipleDocuments_ShouldReturnAllOrderedByCreatedAt()
        {
            // Arrange
            var document1 = new ReceiptDocument(Guid.NewGuid(), "https://cloudinary.com/receipt1.pdf", "receipt1");
            var document2 = new ReceiptDocument(Guid.NewGuid(), "https://cloudinary.com/receipt2.pdf", "receipt2");
            var document3 = new ReceiptDocument(Guid.NewGuid(), "https://cloudinary.com/receipt3.pdf", "receipt3");

            _context.ReceiptDocuments.AddRange(document1, document2, document3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(3, result.Count());
                var orderedResult = result.OrderByDescending(rd => rd.CreatedAt).ToList();
                Assert.Equal(orderedResult, result.ToList());
            });
        }

        [Fact]
        public async Task AddAsync_WithValidReceiptDocument_ShouldAddToDatabase()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var receiptDocument = new ReceiptDocument(transactionId, "https://cloudinary.com/new-receipt.pdf", "new-receipt");

            // Act
            var result = await _repository.AddAsync(receiptDocument);

            // Assert
            Assert.Equal(receiptDocument, result);
            
            var saved = await _context.ReceiptDocuments.FindAsync(receiptDocument.Id);
            Assert.Multiple(() =>
            {
                Assert.NotNull(saved);
                Assert.Equal(transactionId, saved.TransactionId);
                Assert.Equal("https://cloudinary.com/new-receipt.pdf", saved.DocumentUrl);
            });
        }

        [Fact]
        public async Task UpdateAsync_WithExistingReceiptDocument_ShouldUpdateDatabase()
        {
            // Arrange
            var receiptDocument = new ReceiptDocument(Guid.NewGuid(), "https://cloudinary.com/old-receipt.pdf", "old-receipt");
            _context.ReceiptDocuments.Add(receiptDocument);
            await _context.SaveChangesAsync();

            // Modify using reflection to simulate an update
            var field = typeof(ReceiptDocument).GetField("<DocumentUrl>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(receiptDocument, "https://cloudinary.com/updated-receipt.pdf");

            // Act
            await _repository.UpdateAsync(receiptDocument);

            // Assert
            var updated = await _context.ReceiptDocuments.FindAsync(receiptDocument.Id);
            Assert.Multiple(() =>
            {
                Assert.NotNull(updated);
                Assert.Equal("https://cloudinary.com/updated-receipt.pdf", updated.DocumentUrl);
            });
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_ShouldRemoveFromDatabase()
        {
            // Arrange
            var receiptDocument = new ReceiptDocument(Guid.NewGuid(), "https://cloudinary.com/delete-me.pdf", "delete-me");
            _context.ReceiptDocuments.Add(receiptDocument);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(receiptDocument.Id);

            // Assert
            var deleted = await _context.ReceiptDocuments.FindAsync(receiptDocument.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldNotThrow()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            // Should not throw - if we get here without exception, test passes
            await _repository.DeleteAsync(nonExistentId);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            var receiptDocument = new ReceiptDocument(Guid.NewGuid(), "https://cloudinary.com/exists.pdf", "exists");
            _context.ReceiptDocuments.Add(receiptDocument);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(receiptDocument.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.ExistsAsync(nonExistentId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExistsByTransactionIdAsync_WithExistingTransactionId_ShouldReturnTrue()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var receiptDocument = new ReceiptDocument(transactionId, "https://cloudinary.com/exists.pdf", "exists");
            _context.ReceiptDocuments.Add(receiptDocument);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsByTransactionIdAsync(transactionId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsByTransactionIdAsync_WithNonExistentTransactionId_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentTransactionId = Guid.NewGuid();

            // Act
            var result = await _repository.ExistsByTransactionIdAsync(nonExistentTransactionId);

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
