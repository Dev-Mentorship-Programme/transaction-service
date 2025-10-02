using System;
using FluentAssertions;
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
            receiptDocument.Id.Should().NotBeEmpty();
            receiptDocument.TransactionId.Should().Be(transactionId);
            receiptDocument.DocumentUrl.Should().Be(documentUrl);
            receiptDocument.CloudinaryPublicId.Should().Be(cloudinaryPublicId);
            receiptDocument.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var receiptDocument = new ReceiptDocument();

            // Assert
            receiptDocument.Id.Should().NotBeEmpty();
            receiptDocument.TransactionId.Should().BeEmpty();
            receiptDocument.DocumentUrl.Should().Be(string.Empty);
            receiptDocument.CloudinaryPublicId.Should().Be(string.Empty);
            receiptDocument.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Constructor_WithEmptyTransactionId_ShouldThrowArgumentException()
        {
            // Arrange
            var documentUrl = "https://res.cloudinary.com/demo/receipt123.pdf";
            var cloudinaryPublicId = "receipts/receipt123";

            // Act & Assert
            var action = () => new ReceiptDocument(Guid.Empty, documentUrl, cloudinaryPublicId);
            action.Should().Throw<ArgumentException>()
                .WithMessage("TransactionId cannot be empty*");
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
            var action = () => new ReceiptDocument(transactionId, documentUrl, cloudinaryPublicId);
            action.Should().Throw<ArgumentException>()
                .WithMessage("DocumentUrl cannot be empty*");
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
            var action = () => new ReceiptDocument(transactionId, documentUrl, cloudinaryPublicId);
            action.Should().Throw<ArgumentException>()
                .WithMessage("CloudinaryPublicId cannot be empty*");
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
            receiptDocument.DocumentUrl.Should().Be(documentUrl);
            receiptDocument.CloudinaryPublicId.Should().Be(cloudinaryPublicId);
        }
    }
}
