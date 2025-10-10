using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Services;
using Xunit;

namespace TransactionService.Infrastructure.Tests.Services
{
    public class ReceiptGeneratorServiceTests
    {
        private readonly Mock<ILogger<ReceiptGeneratorService>> _mockLogger;
        private readonly ReceiptGeneratorService _service;

        public ReceiptGeneratorServiceTests()
        {
            _mockLogger = new Mock<ILogger<ReceiptGeneratorService>>();
            _service = new ReceiptGeneratorService(_mockLogger.Object);
        }

        [Fact]
        public async Task GenerateReceiptPdfAsync_WithValidTransaction_ShouldReturnPdfStream()
        {
            // Arrange
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Amount = 1000.50m,
                OpeningBalance = 5000.00m,
                ClosingBalance = 4000.00m,
                Type = TransactionType.DEBIT,
                Status = TransactionStatus.SUCCESS,
                Currency = TransactionCurrency.NGN,
                Channel = TransactionChannel.TRANSFER,
                Narration = "Test payment",
                Reference = "TR-20231001123456",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            using var result = await _service.GenerateReceiptPdfAsync(transaction);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.True(result.CanRead);
                Assert.True(result.Length > 0);
            });
        }

        [Fact]
        public async Task GenerateReceiptPdfAsync_WithNullClosingBalance_ShouldHandleGracefully()
        {
            // Arrange
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Amount = 1000.50m,
                OpeningBalance = 5000.00m,
                ClosingBalance = null, // Null closing balance
                Type = TransactionType.CREDIT,
                Status = TransactionStatus.PENDING,
                Currency = TransactionCurrency.USD,
                Channel = TransactionChannel.POS,
                Narration = "Test credit",
                Reference = "TR-20231001123457",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            using var result = await _service.GenerateReceiptPdfAsync(transaction);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.True(result.CanRead);
                Assert.True(result.Length > 0);
            });
        }

        [Fact]
        public async Task GenerateReceiptPdfAsync_WithDifferentCurrencies_ShouldHandleAllCurrencies()
        {
            // Arrange
            var transactionNGN = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Amount = 1000.00m,
                OpeningBalance = 5000.00m,
                Type = TransactionType.DEBIT,
                Status = TransactionStatus.SUCCESS,
                Currency = TransactionCurrency.NGN,
                Channel = TransactionChannel.TRANSFER,
                Narration = "NGN payment",
                Reference = "TR-NGN-123",
                CreatedAt = DateTime.UtcNow
            };

            var transactionUSD = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Amount = 100.00m,
                OpeningBalance = 500.00m,
                Type = TransactionType.CREDIT,
                Status = TransactionStatus.SUCCESS,
                Currency = TransactionCurrency.USD,
                Channel = TransactionChannel.VIRTUAL_ACCOUNT,
                Narration = "USD payment",
                Reference = "TR-USD-123",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            using var resultNGN = await _service.GenerateReceiptPdfAsync(transactionNGN);
            using var resultUSD = await _service.GenerateReceiptPdfAsync(transactionUSD);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(resultNGN);
                Assert.NotNull(resultUSD);
                Assert.True(resultNGN.Length > 0);
                Assert.True(resultUSD.Length > 0);
            });
        }

        [Fact]
        public async Task GenerateReceiptPdfAsync_WithLongNarration_ShouldHandleGracefully()
        {
            // Arrange
            var longNarration = new string('A', 500); // Max length narration
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Amount = 1000.00m,
                OpeningBalance = 5000.00m,
                Type = TransactionType.DEBIT,
                Status = TransactionStatus.SUCCESS,
                Currency = TransactionCurrency.NGN,
                Channel = TransactionChannel.BILL_PAYMENT,
                Narration = longNarration,
                Reference = "TR-LONG-123",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            using var result = await _service.GenerateReceiptPdfAsync(transaction);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.True(result.Length > 0);
            });
        }
    }
}
