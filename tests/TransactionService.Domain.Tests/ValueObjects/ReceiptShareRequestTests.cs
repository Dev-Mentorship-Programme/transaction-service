using System;
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
            Assert.Multiple(() =>
            {
                Assert.Equal(transactionId, request.TransactionId);
                Assert.Equal(expirationHours, request.ExpirationHours);
                Assert.Equal(requestedBy, request.RequestedBy);
            });
        }

        [Fact]
        public void Constructor_WithEmptyTransactionId_ShouldThrowArgumentException()
        {
            // Arrange
            var expirationHours = 24;
            var requestedBy = "user@example.com";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new ReceiptShareRequest(Guid.Empty, expirationHours, requestedBy));
            Assert.StartsWith("TransactionId cannot be empty", exception.Message);
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
            var exception = Assert.Throws<ArgumentException>(
                () => new ReceiptShareRequest(transactionId, expirationHours, requestedBy));
            Assert.StartsWith("ExpirationHours must be between 1 and 168 hours", exception.Message);
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
            var exception = Assert.Throws<ArgumentException>(
                () => new ReceiptShareRequest(transactionId, expirationHours, requestedBy));
            Assert.StartsWith("RequestedBy cannot be empty", exception.Message);
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
            // No exception should be thrown - test passes if we reach this point
            var request = new ReceiptShareRequest(transactionId, expirationHours, requestedBy);
            Assert.NotNull(request);
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
            Assert.True(Math.Abs((actualExpiration - expectedExpiration).TotalSeconds) < 1);
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
            Assert.Multiple(() =>
            {
                Assert.Equal(request1, request2);
                Assert.Equal(request1.GetHashCode(), request2.GetHashCode());
            });
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
            Assert.NotEqual(request1, request2);
        }
    }
}
