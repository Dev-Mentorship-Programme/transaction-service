using System;
using FluentAssertions;
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
            signedLink.Id.Should().NotBeEmpty();
            signedLink.TransactionId.Should().Be(transactionId);
            signedLink.ShareableUrl.Should().Be(shareableUrl);
            signedLink.ExpiresAt.Should().Be(expiresAt);
            signedLink.ResourceType.Should().Be("Receipt");
            signedLink.IsActive.Should().BeTrue();
            signedLink.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Constructor_WithEmptyTransactionId_ShouldThrowArgumentException()
        {
            // Arrange
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Act & Assert
            var action = () => new SignedLink(Guid.Empty, shareableUrl, expiresAt);
            action.Should().Throw<ArgumentException>()
                .WithMessage("TransactionId cannot be empty*");
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
            var action = () => new SignedLink(transactionId, shareableUrl, expiresAt);
            action.Should().Throw<ArgumentException>()
                .WithMessage("ShareableUrl cannot be empty*");
        }

        [Fact]
        public void Constructor_WithPastExpirationDate_ShouldThrowArgumentException()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var shareableUrl = "https://example.com/receipt/abc123";
            var expiresAt = DateTime.UtcNow.AddHours(-1);

            // Act & Assert
            var action = () => new SignedLink(transactionId, shareableUrl, expiresAt);
            action.Should().Throw<ArgumentException>()
                .WithMessage("ExpiresAt must be in the future*");
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
            signedLink.IsExpired.Should().BeFalse();
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
            signedLink.IsExpired.Should().BeTrue();
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
            signedLink.IsActive.Should().BeFalse();
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
            signedLink.IsValid.Should().BeTrue();
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
            signedLink.IsValid.Should().BeFalse();
        }
    }
}
