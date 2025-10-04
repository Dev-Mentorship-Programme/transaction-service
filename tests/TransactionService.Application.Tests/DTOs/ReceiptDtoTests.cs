using System;
using TransactionService.Application.DTOs;
using Xunit;

namespace TransactionService.Application.Tests.DTOs
{
    public class ReceiptDtoTests
    {
        [Fact]
        public void Constructor_WithAllParameters_ShouldCreateDto()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);
            var documentUrl = "https://cloudinary.com/receipt.pdf";

            // Act
            var dto = new ReceiptDto(transactionId, shareableUrl, expiresAt, documentUrl);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(transactionId, dto.TransactionId);
                Assert.Equal(shareableUrl, dto.ShareableUrl);
                Assert.Equal(expiresAt, dto.ExpiresAt);
                Assert.Equal(documentUrl, dto.DocumentUrl);
            });
        }

        [Fact]
        public void Constructor_WithoutDocumentUrl_ShouldHaveNullDocumentUrl()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Act
            var dto = new ReceiptDto(transactionId, shareableUrl, expiresAt);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(transactionId, dto.TransactionId);
                Assert.Equal(shareableUrl, dto.ShareableUrl);
                Assert.Equal(expiresAt, dto.ExpiresAt);
                Assert.Null(dto.DocumentUrl);
            });
        }

        [Fact]
        public void RecordEquality_WithSameValues_ShouldBeEqual()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);
            var documentUrl = "https://cloudinary.com/receipt.pdf";

            // Act
            var dto1 = new ReceiptDto(transactionId, shareableUrl, expiresAt, documentUrl);
            var dto2 = new ReceiptDto(transactionId, shareableUrl, expiresAt, documentUrl);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(dto1, dto2);
                Assert.Equal(dto1.GetHashCode(), dto2.GetHashCode());
            });
        }

        [Fact]
        public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var transactionId1 = Guid.NewGuid();
            var transactionId2 = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Act
            var dto1 = new ReceiptDto(transactionId1, shareableUrl, expiresAt);
            var dto2 = new ReceiptDto(transactionId2, shareableUrl, expiresAt);

            // Assert
            Assert.NotEqual(dto1, dto2);
        }
    }
}
