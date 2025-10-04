using System;
using TransactionService.Domain.Entities;
using Xunit;

namespace TransactionService.Domain.Tests.Entities
{
    public class SignedLinkTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateSignedLink()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Act
            var signedLink = new SignedLink(transactionId, shareableUrl, expiresAt);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotEqual(Guid.Empty, signedLink.Id);
                Assert.Equal(transactionId, signedLink.TransactionId);
                Assert.Equal(shareableUrl, signedLink.ShareableUrl);
                Assert.Equal(expiresAt, signedLink.ExpiresAt);
                Assert.Equal("Receipt", signedLink.ResourceType);
                Assert.True(signedLink.IsActive);
                Assert.True(Math.Abs((signedLink.CreatedAt - DateTime.UtcNow).TotalSeconds) < 1);
            });
        }

        [Fact]
        public void Constructor_WithEmptyTransactionId_ShouldThrowArgumentException()
        {
            // Arrange
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new SignedLink(Guid.Empty, shareableUrl, expiresAt));
            Assert.StartsWith("TransactionId cannot be empty", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidShareableUrl_ShouldThrowArgumentException(string shareableUrl)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new SignedLink(transactionId, shareableUrl, expiresAt));
            Assert.StartsWith("ShareableUrl cannot be empty", exception.Message);
        }

        [Fact]
        public void Constructor_WithPastExpirationDate_ShouldThrowArgumentException()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(-1);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new SignedLink(transactionId, shareableUrl, expiresAt));
            Assert.StartsWith("ExpiresAt must be in the future", exception.Message);
        }

        [Fact]
        public void IsExpired_WhenNotExpired_ShouldReturnFalse()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);
            var signedLink = new SignedLink(transactionId, shareableUrl, expiresAt);

            // Act & Assert
            Assert.False(signedLink.IsExpired);
        }

        [Fact]
        public void IsExpired_WhenExpired_ShouldReturnTrue()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddMilliseconds(1);
            var signedLink = new SignedLink(transactionId, shareableUrl, expiresAt);

            // Act
            System.Threading.Thread.Sleep(10); // Wait for expiration
            
            // Assert
            Assert.True(signedLink.IsExpired);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);
            var signedLink = new SignedLink(transactionId, shareableUrl, expiresAt);

            // Act
            signedLink.Deactivate();

            // Assert
            Assert.False(signedLink.IsActive);
        }

        [Fact]
        public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);
            var signedLink = new SignedLink(transactionId, shareableUrl, expiresAt);

            // Act & Assert
            Assert.True(signedLink.IsValid);
        }

        [Fact]
        public void IsValid_WhenDeactivated_ShouldReturnFalse()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);
            var signedLink = new SignedLink(transactionId, shareableUrl, expiresAt);

            // Act
            signedLink.Deactivate();

            // Assert
            Assert.False(signedLink.IsValid);
        }
    }
}
