using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionService.Application.DTOs;
using TransactionService.Application.Handlers;
using TransactionService.Application.Interfaces;
using TransactionService.Application.Queries;
using TransactionService.Domain.Entities;
using TransactionService.Domain.ValueObjects;
using Xunit;

namespace TransactionService.Application.Tests.Handlers
{
    public class GetTransactionReceiptQueryHandlerTests
    {
        private readonly Mock<ITransactionRepository> _mockTransactionRepository;
        private readonly Mock<ISignedLinkRepository> _mockSignedLinkRepository;
        private readonly Mock<IReceiptDocumentRepository> _mockReceiptDocumentRepository;
        private readonly Mock<IReceiptService> _mockReceiptService;
        private readonly Mock<ILogger<GetTransactionReceiptQueryHandler>> _mockLogger;
        private readonly GetTransactionReceiptQueryHandler _handler;

        public GetTransactionReceiptQueryHandlerTests()
        {
            _mockTransactionRepository = new Mock<ITransactionRepository>();
            _mockSignedLinkRepository = new Mock<ISignedLinkRepository>();
            _mockReceiptDocumentRepository = new Mock<IReceiptDocumentRepository>();
            _mockReceiptService = new Mock<IReceiptService>();
            _mockLogger = new Mock<ILogger<GetTransactionReceiptQueryHandler>>();
            _handler = new GetTransactionReceiptQueryHandler(
                _mockTransactionRepository.Object,
                _mockSignedLinkRepository.Object,
                _mockReceiptDocumentRepository.Object,
                _mockReceiptService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithNonExistentTransaction_ShouldReturnNull()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var query = new GetTransactionReceiptQuery(transactionId, "user@example.com");

            _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Transaction?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_WithExistingValidLink_ShouldReturnExistingReceipt()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var query = new GetTransactionReceiptQuery(transactionId, "user@example.com");
            
            var transaction = new Transaction { Id = transactionId };
            var existingLink = new SignedLink(transactionId, "https://example.com/receipt/existing", DateTime.UtcNow.AddHours(24));
            var receiptDocument = new ReceiptDocument(transactionId, "https://cloudinary.com/receipt.pdf", "receipt123");

            _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transaction);

            _mockSignedLinkRepository.Setup(r => r.GetActiveByTransactionIdAsync(transactionId, "Receipt", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLink);

            _mockReceiptDocumentRepository.Setup(r => r.GetByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(receiptDocument);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(transactionId, result.TransactionId);
                Assert.Equal(existingLink.ShareableUrl, result.ShareableUrl);
                Assert.Equal(existingLink.ExpiresAt, result.ExpiresAt);
                Assert.Equal(receiptDocument.DocumentUrl, result.DocumentUrl);
            });
        }

        [Fact]
        public async Task Handle_WithNoExistingLink_ShouldGenerateNewReceipt()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var query = new GetTransactionReceiptQuery(transactionId, "user@example.com", 48);
            
            var transaction = new Transaction { Id = transactionId };
            var newLink = new SignedLink(transactionId, "https://example.com/receipt/new", DateTime.UtcNow.AddHours(48));

            _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transaction);

            _mockSignedLinkRepository.Setup(r => r.GetActiveByTransactionIdAsync(transactionId, "Receipt", It.IsAny<CancellationToken>()))
                .ReturnsAsync((SignedLink?)null);

            _mockReceiptDocumentRepository.Setup(r => r.GetByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReceiptDocument?)null);

            _mockReceiptService.Setup(s => s.GetShareableLinkAsync(It.IsAny<ReceiptShareRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newLink);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(transactionId, result.TransactionId);
                Assert.Equal(newLink.ShareableUrl, result.ShareableUrl);
                Assert.Equal(newLink.ExpiresAt, result.ExpiresAt);
            });

            _mockReceiptService.Verify(s => s.GetShareableLinkAsync(
                It.Is<ReceiptShareRequest>(r => 
                    r.TransactionId == transactionId && 
                    r.ExpirationHours == 48 && 
                    r.RequestedBy == "user@example.com"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidExistingLink_ShouldGenerateNewReceipt()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var query = new GetTransactionReceiptQuery(transactionId, "user@example.com");
            
            var transaction = new Transaction { Id = transactionId };
            var newLink = new SignedLink(transactionId, "https://example.com/receipt/new", DateTime.UtcNow.AddHours(24));

            _mockTransactionRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transaction);

            // Repository will return null for expired/invalid links
            _mockSignedLinkRepository.Setup(r => r.GetActiveByTransactionIdAsync(transactionId, "Receipt", It.IsAny<CancellationToken>()))
                .ReturnsAsync((SignedLink?)null);

            _mockReceiptDocumentRepository.Setup(r => r.GetByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReceiptDocument?)null);

            _mockReceiptService.Setup(s => s.GetShareableLinkAsync(It.IsAny<ReceiptShareRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newLink);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.Equal(newLink.ShareableUrl, result.ShareableUrl);
            });
            _mockReceiptService.Verify(s => s.GetShareableLinkAsync(It.IsAny<ReceiptShareRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
