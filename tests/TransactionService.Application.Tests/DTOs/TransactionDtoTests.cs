using TransactionService.Application.DTOs;
using TransactionService.Domain.Entities;
using Xunit;

namespace TransactionService.Application.Tests.DTOs
{
    public class TransactionDtoTests
    {
        [Fact]
        public void Can_Create_TransactionDto_With_Required_Properties()
        {
            var id = Guid.NewGuid();
            // Arrange
            var dto = new TransactionDto
            {
                Id = id,
                AccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Amount = 5000m,
                OpeningBalance = 10000m,
                Currency = TransactionCurrency.NGN,
                Status = TransactionStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            // Act & Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal(TransactionCurrency.NGN, dto.Currency);
            Assert.Equal(TransactionStatus.PENDING, dto.Status);
            Assert.Equal(5000m, dto.Amount);
        }
    }
}
