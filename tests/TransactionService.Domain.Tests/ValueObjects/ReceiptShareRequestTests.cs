using System;
using FluentAssertions;
using TransactionService.Domain.ValueObjects;
using Xunit;

namespace TransactionService.Domain.Tests.ValueObjects
{
    public class ReceiptShareRequestTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateReceiptShareRequest()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var expirationHours = 24;
            var requestedBy = "user@example.com";

            // Act
            var request = new ReceiptShareRequest(transactionId, expirationHours, requestedBy);

            // Assert
            request.TransactionId.Should().Be(transactionId);
            request.ExpirationHours.Should().Be(expirationHours);
            request.RequestedBy.Should().Be(requestedBy);
        }

        [Fact]
        public void Constructor_WithEmptyTransactionId_ShouldThrowArgumentException()
        {
            // Arrange
            var expirationHours = 24;
            var requestedBy = "user@example.com";

            // Act & Assert
            var action = () => new ReceiptShareRequest(Guid.Empty, expirationHours, requestedBy);
            action.Should().Throw<ArgumentException>()
                .WithMessage("TransactionId cannot be empty*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(169)] // More than 168 hours (7 days)
        public void Constructor_WithInvalidExpirationHours_ShouldThrowArgumentException(int expirationHours)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";

            // Act & Assert
            var action = () => new ReceiptShareRequest(transactionId, expirationHours, requestedBy);
            action.Should().Throw<ArgumentException>()
                .WithMessage("ExpirationHours must be between 1 and 168 hours*");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidRequestedBy_ShouldThrowArgumentException(string requestedBy)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var expirationHours = 24;

            // Act & Assert
            var action = () => new ReceiptShareRequest(transactionId, expirationHours, requestedBy);
            action.Should().Throw<ArgumentException>()
                .WithMessage("RequestedBy cannot be empty*");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(24)]
        [InlineData(168)] // Exactly 7 days
        public void Constructor_WithValidExpirationHours_ShouldNotThrow(int expirationHours)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";

            // Act
            var action = () => new ReceiptShareRequest(transactionId, expirationHours, requestedBy);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void CalculateExpirationDate_ShouldReturnCorrectDate()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var expirationHours = 24;
            var requestedBy = "user@example.com";
            var request = new ReceiptShareRequest(transactionId, expirationHours, requestedBy);
            var expectedExpiration = DateTime.UtcNow.AddHours(expirationHours);

            // Act
            var actualExpiration = request.CalculateExpirationDate();

            // Assert
            actualExpiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void RecordEquality_WithSameValues_ShouldBeEqual()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var expirationHours = 24;
            var requestedBy = "user@example.com";

            // Act
            var request1 = new ReceiptShareRequest(transactionId, expirationHours, requestedBy);
            var request2 = new ReceiptShareRequest(transactionId, expirationHours, requestedBy);

            // Assert
            request1.Should().Be(request2);
            request1.GetHashCode().Should().Be(request2.GetHashCode());
        }

        [Fact]
        public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var transactionId1 = Guid.NewGuid();
            var transactionId2 = Guid.NewGuid();
            var expirationHours = 24;
            var requestedBy = "user@example.com";

            // Act
            var request1 = new ReceiptShareRequest(transactionId1, expirationHours, requestedBy);
            var request2 = new ReceiptShareRequest(transactionId2, expirationHours, requestedBy);

            // Assert
            request1.Should().NotBe(request2);
        }
    }
}
