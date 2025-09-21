using System;
using Xunit;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Factories;

namespace TransactionService.Domain.Tests.Factories
{
    public class TransactionFactoryTests
    {
        private readonly TransactionFactory _factory = new();

        [Fact]
        public void Create_ShouldReturnTransaction_WithExpectedValues()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var destinationAccountId = Guid.NewGuid();
            var amount = 1000m;
            var openingBalance = 5000m;
            var closingBalance = 4000m;
            var narration = "Test Transaction";
            var type = TransactionType.DEBIT;
            var channel = TransactionChannel.TRANSFER;
            var currency = TransactionCurrency.NGN;
            var reference = "REF123";
            var metadata = "{\"key\":\"value\"}";

            // Act
            var transaction = _factory.Create(
                accountId,
                destinationAccountId,
                amount,
                openingBalance,
                closingBalance,
                narration,
                type,
                channel,
                currency,
                reference,
                metadata);

            // Assert
            Assert.Equal(accountId, transaction.AccountId);
            Assert.Equal(destinationAccountId, transaction.DestinationAccountId);
            Assert.Equal(amount, transaction.Amount);
            Assert.Equal(openingBalance, transaction.OpeningBalance);
            Assert.Equal(closingBalance, transaction.ClosingBalance);
            Assert.Equal(narration, transaction.Narration);
            Assert.Equal(type, transaction.Type);
            Assert.Equal(channel, transaction.Channel);
            Assert.Equal(currency, transaction.Currency);
            Assert.Equal(reference, transaction.Reference);
            Assert.Equal(metadata, transaction.Metadata);
            Assert.Equal(TransactionStatus.PENDING, transaction.Status);
            Assert.True(transaction.CreatedAt <= DateTime.UtcNow);
            Assert.True(transaction.UpdatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_ShouldGenerateReference_WhenReferenceIsNull()
        {
            // Act
            var transaction = _factory.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1000m,
                5000m,
                4000m,
                "AutoRef Test",
                TransactionType.CREDIT,
                TransactionChannel.BILL_PAYMENT,
                TransactionCurrency.USD,
                null,
                null);

            // Assert
            Assert.StartsWith("TR-", transaction.Reference);
            Assert.Null(transaction.Metadata);
        }

        [Fact]
        public void Update_ShouldModifyTransactionFields()
        {
            // Arrange
            var transaction = _factory.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1000m,
                5000m,
                4000m,
                "Initial Narration",
                TransactionType.DEBIT,
                TransactionChannel.TRANSFER,
                TransactionCurrency.NGN,
                "REF123",
                "{\"initial\":\"data\"}");

            var newStatus = TransactionStatus.SUCCESS;
            var newClosingBalance = 4500m;
            var newNarration = "Updated Narration";
            var newMetadata = "{\"updated\":\"data\"}";

            // Act
            var updatedTransaction = _factory.Update(
                transaction,
                newStatus,
                newClosingBalance,
                newNarration,
                newMetadata);

            // Assert
            Assert.Equal(newStatus, updatedTransaction.Status);
            Assert.Equal(newClosingBalance, updatedTransaction.ClosingBalance);
            Assert.Equal(newNarration, updatedTransaction.Narration);
            Assert.Equal(newMetadata, updatedTransaction.Metadata);
            Assert.True(updatedTransaction.UpdatedAt > transaction.CreatedAt);
        }

        [Fact]
        public void Update_ShouldIgnoreNullOrEmptyOptionalFields()
        {
            // Arrange
            var transaction = _factory.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1000m,
                5000m,
                4000m,
                "Original Narration",
                TransactionType.DEBIT,
                TransactionChannel.TRANSFER,
                TransactionCurrency.NGN,
                "REF123",
                "{\"original\":\"data\"}");

            var originalUpdatedAt = transaction.UpdatedAt;

            // Act
            var updatedTransaction = _factory.Update(
                transaction,
                TransactionStatus.FAILED,
                null,
                null,
                "");

            // Assert
            Assert.Equal("Original Narration", updatedTransaction.Narration);
            Assert.Equal("{\"original\":\"data\"}", updatedTransaction.Metadata);
            Assert.Equal(4000m, updatedTransaction.ClosingBalance);
            Assert.Equal(TransactionStatus.FAILED, updatedTransaction.Status);
            Assert.True(updatedTransaction.UpdatedAt > originalUpdatedAt);
        }


        [Fact]
        public void Create_ShouldThrow_WhenAmountIsNegative()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var destinationAccountId = Guid.NewGuid();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _factory.Create(
                    accountId,
                    destinationAccountId,
                    -1000m, // Invalid amount
                    5000m,
                    4000m,
                    "Negative amount",
                    TransactionType.DEBIT,
                    TransactionChannel.TRANSFER,
                    TransactionCurrency.NGN));
        }

        [Fact]
        public void Create_ShouldThrow_WhenClosingBalanceIsGreaterThanOpeningBalance_ForDebit()
        {
            var accountId = Guid.NewGuid();
            var destinationAccountId = Guid.NewGuid();

            Assert.Throws<InvalidOperationException>(() =>
                _factory.Create(
                    accountId,
                    destinationAccountId,
                    1000m,
                    5000m,
                    6000m, // Invalid closing balance for debit
                    "Invalid balance",
                    TransactionType.DEBIT,
                    TransactionChannel.TRANSFER,
                    TransactionCurrency.NGN));
        }

        [Fact]
        public void Update_ShouldThrow_WhenStatusIsInvalidEnum()
        {
            var transaction = _factory.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1000m,
                5000m,
                4000m,
                "Initial",
                TransactionType.DEBIT,
                TransactionChannel.TRANSFER,
                TransactionCurrency.NGN);

            var invalidStatus = (TransactionStatus)999;

            Assert.Throws<ArgumentException>(() =>
                _factory.Update(transaction, invalidStatus));
        }
    }
}
