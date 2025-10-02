using System;
using FluentAssertions;
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
            query.TransactionId.Should().Be(transactionId);
            query.RequestedBy.Should().Be(requestedBy);
            query.ExpirationHours.Should().Be(expirationHours);
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
            query.ExpirationHours.Should().Be(24);
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
            query1.Should().Be(query2);
            query1.GetHashCode().Should().Be(query2.GetHashCode());
        }
    }
}
