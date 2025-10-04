using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Data;
using Xunit;

namespace TransactionService.Infrastructure.Tests.Data
{
    public class AppDbContextTests : IDisposable
    {
        private readonly AppDbContext _context;

        public AppDbContextTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
        }

        [Fact]
        public void DbContext_ShouldHaveSignedLinksDbSet()
        {
            // Assert
            Assert.NotNull(_context.SignedLinks);
        }

        [Fact]
        public void DbContext_ShouldHaveReceiptDocumentsDbSet()
        {
            // Assert
            Assert.NotNull(_context.ReceiptDocuments);
        }

        [Fact]
        public void SignedLink_ShouldBeAddedToDatabase()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);
            var signedLink = new SignedLink(transactionId, shareableUrl, expiresAt);

            // Act
            _context.SignedLinks.Add(signedLink);
            _context.SaveChanges();

            // Assert
            var retrievedLink = _context.SignedLinks.First();
            Assert.Multiple(() =>
            {
                Assert.Equal(transactionId, retrievedLink.TransactionId);
                Assert.Equal(shareableUrl, retrievedLink.ShareableUrl);
                Assert.True(Math.Abs((retrievedLink.ExpiresAt - expiresAt).TotalMilliseconds) < 1);
            });
        }

        [Fact]
        public void ReceiptDocument_ShouldBeAddedToDatabase()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var documentUrl = "https://res.cloudinary.com/demo/receipt123.pdf";
            var cloudinaryPublicId = "receipts/receipt123";
            var receiptDocument = new ReceiptDocument(transactionId, documentUrl, cloudinaryPublicId);

            // Act
            _context.ReceiptDocuments.Add(receiptDocument);
            _context.SaveChanges();

            // Assert
            var retrievedDocument = _context.ReceiptDocuments.First();
            Assert.Multiple(() =>
            {
                Assert.Equal(transactionId, retrievedDocument.TransactionId);
                Assert.Equal(documentUrl, retrievedDocument.DocumentUrl);
                Assert.Equal(cloudinaryPublicId, retrievedDocument.CloudinaryPublicId);
            });
        }

        [Fact]
        public void SignedLink_ShouldHaveUniqueShareableUrlConfigured()
        {
            // Arrange & Act
            var entityType = _context.Model.FindEntityType(typeof(SignedLink));
            var shareableUrlIndex = entityType?.GetIndexes()
                .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(SignedLink.ShareableUrl)));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(shareableUrlIndex);
                Assert.True(shareableUrlIndex.IsUnique);
            });
        }

        [Fact]
        public void Entities_ShouldQueryByTransactionId()
        {
            // Arrange
            var targetTransactionId = Guid.NewGuid();
            var otherTransactionId = Guid.NewGuid();
            
            var signedLink = new SignedLink(targetTransactionId, "https://example.com/receipt/1", DateTime.UtcNow.AddHours(24));
            var receiptDocument = new ReceiptDocument(targetTransactionId, "https://example.com/doc1.pdf", "doc1");
            var otherSignedLink = new SignedLink(otherTransactionId, "https://example.com/receipt/2", DateTime.UtcNow.AddHours(24));

            _context.SignedLinks.AddRange(signedLink, otherSignedLink);
            _context.ReceiptDocuments.Add(receiptDocument);
            _context.SaveChanges();

            // Act
            var signedLinksForTransaction = _context.SignedLinks
                .Where(sl => sl.TransactionId == targetTransactionId)
                .ToList();

            var receiptDocumentsForTransaction = _context.ReceiptDocuments
                .Where(rd => rd.TransactionId == targetTransactionId)
                .ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Single(signedLinksForTransaction);
                Assert.Equal(targetTransactionId, signedLinksForTransaction.First().TransactionId);
                Assert.Single(receiptDocumentsForTransaction);
                Assert.Equal(targetTransactionId, receiptDocumentsForTransaction.First().TransactionId);
            });
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
