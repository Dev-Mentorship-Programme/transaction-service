using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Commands;
using TransactionService.Application.Handlers;
using TransactionService.Application.Interfaces;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Application.Tests.Handlers
{
    public class CreateTransactionCommandHandlerTests
    {
        private readonly Mock<ITransactionFactory> _factoryMock = new();
        private readonly Mock<ITransactionRepository> _repositoryMock = new();
        private readonly Mock<ILogger<CreateTransactionCommandHandler>> _loggerMock = new();

        private CreateTransactionCommandHandler CreateHandler() =>
            new(_factoryMock.Object, _repositoryMock.Object, _loggerMock.Object);

        [Fact]
        public async Task Handle_ReturnsExistingTransactionId_IfTransactionAlreadyExists()
        {
            // Arrange
            var existingId = Guid.NewGuid();
            var command = new CreateTransactionCommand
            {
                TransactionId = existingId,
                AccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Amount = 1000,
                OpeningBalance = 5000,
                Type = TransactionType.DEBIT,
                Currency = TransactionCurrency.NGN,
                Channel = TransactionChannel.TRANSFER
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(existingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Transaction { Id = existingId });

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(existingId, result);
            _repositoryMock.Verify(r => r.GetByIdAsync(existingId, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CreatesNewTransaction_IfNotExists()
        {
            // Arrange
            var command = new CreateTransactionCommand
            {
                AccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Amount = 1000,
                OpeningBalance = 5000,
                Type = TransactionType.DEBIT,
                Currency = TransactionCurrency.NGN,
                Channel = TransactionChannel.TRANSFER
            };

            var newTransaction = new Transaction { Id = Guid.NewGuid() };

            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Transaction?)null);

            _factoryMock.Setup(f => f.Create(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<TransactionType>(),
                It.IsAny<TransactionChannel>(),
                It.IsAny<TransactionCurrency>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
                .Returns(newTransaction);


            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(newTransaction.Id, result);
            _repositoryMock.Verify(r => r.AddAsync(newTransaction, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}