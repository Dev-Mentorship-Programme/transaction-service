using System;
using System.Linq;
using FluentAssertions;
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
            _context.SignedLinks.Should().NotBeNull();
        }

        [Fact]
        public void DbContext_ShouldHaveReceiptDocumentsDbSet()
        {
            // Assert
            _context.ReceiptDocuments.Should().NotBeNull();
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
            retrievedLink.TransactionId.Should().Be(transactionId);
            retrievedLink.ShareableUrl.Should().Be(shareableUrl);
            retrievedLink.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromMilliseconds(1));
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
            retrievedDocument.TransactionId.Should().Be(transactionId);
            retrievedDocument.DocumentUrl.Should().Be(documentUrl);
            retrievedDocument.CloudinaryPublicId.Should().Be(cloudinaryPublicId);
        }

        [Fact]
        public void SignedLink_ShouldHaveUniqueShareableUrlConfigured()
        {
            // Arrange & Act
            var entityType = _context.Model.FindEntityType(typeof(SignedLink));
            var shareableUrlIndex = entityType?.GetIndexes()
                .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(SignedLink.ShareableUrl)));

            // Assert
            shareableUrlIndex.Should().NotBeNull();
            shareableUrlIndex!.IsUnique.Should().BeTrue();
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
            signedLinksForTransaction.Should().HaveCount(1);
            signedLinksForTransaction.First().TransactionId.Should().Be(targetTransactionId);

            receiptDocumentsForTransaction.Should().HaveCount(1);
            receiptDocumentsForTransaction.First().TransactionId.Should().Be(targetTransactionId);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
