using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionService.Application.Interfaces;
using TransactionService.Domain.Entities;
using TransactionService.Domain.ValueObjects;
using TransactionService.Infrastructure.Interfaces;
using TransactionService.Infrastructure.Services;
using Xunit;

namespace TransactionService.Infrastructure.Tests.Services
{
    public class ReceiptServiceTests
    {
        private readonly Mock<IAppDbContext> _mockContext;
        private readonly Mock<ICloudinaryService> _mockCloudinaryService;
        private readonly Mock<IReceiptGeneratorService> _mockReceiptGenerator;
        private readonly Mock<ILogger<ReceiptService>> _mockLogger;
        private readonly ReceiptService _service;

        public ReceiptServiceTests()
        {
            _mockContext = new Mock<IAppDbContext>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();
            _mockReceiptGenerator = new Mock<IReceiptGeneratorService>();
            _mockLogger = new Mock<ILogger<ReceiptService>>();
            
            _service = new ReceiptService(
                _mockContext.Object,
                _mockCloudinaryService.Object,
                _mockReceiptGenerator.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateReceiptAsync_WithValidTransactionId_ShouldReturnReceiptDocument()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transaction = new Transaction { Id = transactionId, Reference = "TR-123" };
            var pdfStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("PDF content"));
            var documentUrl = "https://cloudinary.com/receipt.pdf";
            var publicId = "receipts/receipt_123";

            _mockContext.Setup(c => c.Transactions)
                .Returns(new[] { transaction }.AsQueryable());

            _mockContext.Setup(c => c.ReceiptDocuments)
                .Returns(new ReceiptDocument[0].AsQueryable());

            _mockReceiptGenerator.Setup(g => g.GenerateReceiptPdfAsync(transaction, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pdfStream);

            _mockCloudinaryService.Setup(c => c.UploadDocumentAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), "receipts", It.IsAny<CancellationToken>()))
                .ReturnsAsync((documentUrl, publicId));

            // Act
            var result = await _service.GenerateReceiptAsync(transactionId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(transactionId, result.TransactionId);
                Assert.Equal(documentUrl, result.DocumentUrl);
                Assert.Equal(publicId, result.CloudinaryPublicId);
            });
        }

        [Fact]
        public async Task GenerateReceiptAsync_WithNonExistentTransaction_ShouldThrowException()
        {
            // Arrange
            var transactionId = Guid.NewGuid();

            _mockContext.Setup(c => c.Transactions)
                .Returns(new Transaction[0].AsQueryable());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.GenerateReceiptAsync(transactionId));
            Assert.Equal($"Transaction {transactionId} not found", exception.Message);
        }

        [Fact]
        public async Task GenerateReceiptAsync_WithExistingDocument_ShouldReturnExistingDocument()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transaction = new Transaction { Id = transactionId };
            var existingDocument = new ReceiptDocument(transactionId, "https://existing.pdf", "existing123");

            _mockContext.Setup(c => c.Transactions)
                .Returns(new[] { transaction }.AsQueryable());

            _mockContext.Setup(c => c.ReceiptDocuments)
                .Returns(new[] { existingDocument }.AsQueryable());

            // Act
            var result = await _service.GenerateReceiptAsync(transactionId);

            // Assert
            Assert.Equal(existingDocument, result);
            _mockReceiptGenerator.Verify(g => g.GenerateReceiptPdfAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetShareableLinkAsync_WithValidRequest_ShouldReturnSignedLink()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var request = new ReceiptShareRequest(transactionId, 24, "user@example.com");
            var transaction = new Transaction { Id = transactionId, Reference = "TR-123" };
            var receiptDocument = new ReceiptDocument(transactionId, "https://doc.pdf", "doc123");
            var secureUrl = "https://secure.cloudinary.com/doc.pdf";

            _mockContext.Setup(c => c.Transactions)
                .Returns(new[] { transaction }.AsQueryable());

            _mockContext.Setup(c => c.ReceiptDocuments)
                .Returns(new[] { receiptDocument }.AsQueryable());

            _mockCloudinaryService.Setup(c => c.GenerateSecureUrlAsync("doc123", 24, It.IsAny<CancellationToken>()))
                .ReturnsAsync(secureUrl);

            // Act
            var result = await _service.GetShareableLinkAsync(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(transactionId, result.TransactionId);
                Assert.Equal(secureUrl, result.ShareableUrl);
                Assert.Equal("Receipt", result.ResourceType);
                Assert.True(Math.Abs((result.ExpiresAt - DateTime.UtcNow.AddHours(24)).TotalMinutes) < 1);
            });
        }

        [Fact]
        public async Task ValidateLinkAsync_WithValidLink_ShouldReturnTrue()
        {
            // Arrange
            var shareableUrl = "https://example.com/receipt/123";
            var signedLink = new SignedLink(Guid.NewGuid(), shareableUrl, DateTime.UtcNow.AddHours(1));

            _mockContext.Setup(c => c.SignedLinks)
                .Returns(new[] { signedLink }.AsQueryable());

            // Act
            var result = await _service.ValidateLinkAsync(shareableUrl);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateLinkAsync_WithNonExistentLink_ShouldReturnFalse()
        {
            // Arrange
            var shareableUrl = "https://example.com/receipt/nonexistent";

            _mockContext.Setup(c => c.SignedLinks)
                .Returns(new SignedLink[0].AsQueryable());

            // Act
            var result = await _service.ValidateLinkAsync(shareableUrl);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateLinkAsync_WithExpiredLink_ShouldReturnFalse()
        {
            // Arrange
            var shareableUrl = "https://example.com/receipt/expired";
            var expiredLink = new SignedLink
            {
                TransactionId = Guid.NewGuid(),
                ShareableUrl = shareableUrl,
                ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
                ResourceType = "Receipt",
                IsActive = true
            };

            _mockContext.Setup(c => c.SignedLinks)
                .Returns(new[] { expiredLink }.AsQueryable());

            // Act
            var result = await _service.ValidateLinkAsync(shareableUrl);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateLinkAsync_WithInactiveLink_ShouldReturnFalse()
        {
            // Arrange
            var shareableUrl = "https://example.com/receipt/inactive";
            var inactiveLink = new SignedLink(Guid.NewGuid(), shareableUrl, DateTime.UtcNow.AddHours(1));
            inactiveLink.Deactivate();

            _mockContext.Setup(c => c.SignedLinks)
                .Returns(new[] { inactiveLink }.AsQueryable());

            // Act
            var result = await _service.ValidateLinkAsync(shareableUrl);

            // Assert
            Assert.False(result);
        }
    }
}
