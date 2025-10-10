using System;
using TransactionService.Application.Queries;
using Xunit;

namespace TransactionService.Application.Tests.Queries
{
    public class GetTransactionReceiptQueryTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateQuery()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 48;

            // Act
            var query = new GetTransactionReceiptQuery(transactionId, requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(transactionId, query.TransactionId);
                Assert.Equal(requestedBy, query.RequestedBy);
                Assert.Equal(expirationHours, query.ExpirationHours);
            });
        }

        [Fact]
        public void Constructor_WithDefaultExpirationHours_ShouldUse24Hours()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";

            // Act
            var query = new GetTransactionReceiptQuery(transactionId, requestedBy);

            // Assert
            Assert.Equal(24, query.ExpirationHours);
        }

        [Fact]
        public void RecordEquality_WithSameValues_ShouldBeEqual()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 24;

            // Act
            var query1 = new GetTransactionReceiptQuery(transactionId, requestedBy, expirationHours);
            var query2 = new GetTransactionReceiptQuery(transactionId, requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(query1, query2);
                Assert.Equal(query1.GetHashCode(), query2.GetHashCode());
            });
        }
    }
}
