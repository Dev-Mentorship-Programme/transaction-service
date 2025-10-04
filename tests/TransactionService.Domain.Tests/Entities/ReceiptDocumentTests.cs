using System;
using TransactionService.Domain.Entities;
using Xunit;

namespace TransactionService.Domain.Tests.Entities
{
    public class ReceiptDocumentTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateReceiptDocument()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var documentUrl = "https://res.cloudinary.com/demo/receipt123.pdf";
            var cloudinaryPublicId = "receipts/receipt123";

            // Act
            var receiptDocument = new ReceiptDocument(transactionId, documentUrl, cloudinaryPublicId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotEqual(Guid.Empty, receiptDocument.Id);
                Assert.Equal(transactionId, receiptDocument.TransactionId);
                Assert.Equal(documentUrl, receiptDocument.DocumentUrl);
                Assert.Equal(cloudinaryPublicId, receiptDocument.CloudinaryPublicId);
                Assert.True(Math.Abs((receiptDocument.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
            });
        }

        [Fact]
        public void DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var receiptDocument = new ReceiptDocument();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotEqual(Guid.Empty, receiptDocument.Id);
                Assert.Equal(Guid.Empty, receiptDocument.TransactionId);
                Assert.Equal(string.Empty, receiptDocument.DocumentUrl);
                Assert.Equal(string.Empty, receiptDocument.CloudinaryPublicId);
                Assert.True(Math.Abs((receiptDocument.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
            });
        }

        [Fact]
        public void Constructor_WithEmptyTransactionId_ShouldThrowArgumentException()
        {
            // Arrange
            var documentUrl = "https://res.cloudinary.com/demo/receipt123.pdf";
            var cloudinaryPublicId = "receipts/receipt123";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new ReceiptDocument(Guid.Empty, documentUrl, cloudinaryPublicId));
            Assert.StartsWith("TransactionId cannot be empty", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidDocumentUrl_ShouldThrowArgumentException(string documentUrl)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var cloudinaryPublicId = "receipts/receipt123";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new ReceiptDocument(transactionId, documentUrl, cloudinaryPublicId));
            Assert.StartsWith("DocumentUrl cannot be empty", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidCloudinaryPublicId_ShouldThrowArgumentException(string cloudinaryPublicId)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var documentUrl = "https://res.cloudinary.com/demo/receipt123.pdf";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new ReceiptDocument(transactionId, documentUrl, cloudinaryPublicId));
            Assert.StartsWith("CloudinaryPublicId cannot be empty", exception.Message);
        }

        [Fact]
        public void Constructor_WithValidUrls_ShouldAcceptDifferentFormats()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var documentUrl = "https://res.cloudinary.com/drfy6rjlw/image/upload/v123456789/receipts/receipt_abc123.pdf";
            var cloudinaryPublicId = "receipts/receipt_abc123";

            // Act
            var receiptDocument = new ReceiptDocument(transactionId, documentUrl, cloudinaryPublicId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(documentUrl, receiptDocument.DocumentUrl);
                Assert.Equal(cloudinaryPublicId, receiptDocument.CloudinaryPublicId);
            });
        }
    }
}
