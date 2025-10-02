using System;
using System.Linq;
using FluentAssertions;
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
        private readonly Mock<IAppDbContext> _mockContext;
        private readonly Mock<IReceiptService> _mockReceiptService;
        private readonly Mock<ILogger<GetTransactionReceiptQueryHandler>> _mockLogger;
        private readonly GetTransactionReceiptQueryHandler _handler;

        public GetTransactionReceiptQueryHandlerTests()
        {
            _mockContext = new Mock<IAppDbContext>();
            _mockReceiptService = new Mock<IReceiptService>();
            _mockLogger = new Mock<ILogger<GetTransactionReceiptQueryHandler>>();
            _handler = new GetTransactionReceiptQueryHandler(_mockContext.Object, _mockReceiptService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithNonExistentTransaction_ShouldReturnNull()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var query = new GetTransactionReceiptQuery(transactionId, "user@example.com");

            _mockContext.Setup(c => c.Transactions)
                .Returns(new List<Transaction>().AsQueryable());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithExistingValidLink_ShouldReturnExistingReceipt()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var query = new GetTransactionReceiptQuery(transactionId, "user@example.com");
            
            var transaction = new Transaction { Id = transactionId };
            var existingLink = new SignedLink(transactionId, "https://example.com/receipt/123", DateTime.UtcNow.AddHours(24));
            var receiptDocument = new ReceiptDocument(transactionId, "https://cloudinary.com/doc.pdf", "doc123");

            _mockContext.Setup(c => c.Transactions)
                .Returns(new List<Transaction> { transaction }.AsQueryable());

            _mockContext.Setup(c => c.SignedLinks)
                .Returns(new List<SignedLink> { existingLink }.AsQueryable());

            _mockContext.Setup(c => c.ReceiptDocuments)
                .Returns(new List<ReceiptDocument> { receiptDocument }.AsQueryable());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.TransactionId.Should().Be(transactionId);
            result.ShareableUrl.Should().Be(existingLink.ShareableUrl);
            result.ExpiresAt.Should().Be(existingLink.ExpiresAt);
            result.DocumentUrl.Should().Be(receiptDocument.DocumentUrl);
        }

        [Fact]
        public async Task Handle_WithNoExistingLink_ShouldGenerateNewReceipt()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var query = new GetTransactionReceiptQuery(transactionId, "user@example.com", 48);
            
            var transaction = new Transaction { Id = transactionId };
            var newLink = new SignedLink(transactionId, "https://example.com/receipt/new", DateTime.UtcNow.AddHours(48));

            _mockContext.Setup(c => c.Transactions)
                .Returns(new List<Transaction> { transaction }.AsQueryable());

            _mockContext.Setup(c => c.SignedLinks)
                .Returns(new List<SignedLink>().AsQueryable());

            _mockContext.Setup(c => c.ReceiptDocuments)
                .Returns(new List<ReceiptDocument>().AsQueryable());

            _mockReceiptService.Setup(s => s.GetShareableLinkAsync(It.IsAny<ReceiptShareRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newLink);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.TransactionId.Should().Be(transactionId);
            result.ShareableUrl.Should().Be(newLink.ShareableUrl);
            result.ExpiresAt.Should().Be(newLink.ExpiresAt);

            _mockReceiptService.Verify(s => s.GetShareableLinkAsync(
                It.Is<ReceiptShareRequest>(r => 
                    r.TransactionId == transactionId && 
                    r.ExpirationHours == 48 && 
                    r.RequestedBy == "user@example.com"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithExpiredLink_ShouldGenerateNewReceipt()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var query = new GetTransactionReceiptQuery(transactionId, "user@example.com");
            
            var transaction = new Transaction { Id = transactionId };
            
            // Create expired link using default constructor to bypass validation
            var expiredLink = new SignedLink
            {
                TransactionId = transactionId,
                ShareableUrl = "https://example.com/receipt/expired",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                ResourceType = "Receipt",
                IsActive = true
            };
            
            var newLink = new SignedLink(transactionId, "https://example.com/receipt/new", DateTime.UtcNow.AddHours(24));

            _mockContext.Setup(c => c.Transactions)
                .Returns(new List<Transaction> { transaction }.AsQueryable());

            _mockContext.Setup(c => c.SignedLinks)
                .Returns(new List<SignedLink> { expiredLink }.AsQueryable());

            _mockContext.Setup(c => c.ReceiptDocuments)
                .Returns(new List<ReceiptDocument>().AsQueryable());

            _mockReceiptService.Setup(s => s.GetShareableLinkAsync(It.IsAny<ReceiptShareRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newLink);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.ShareableUrl.Should().Be(newLink.ShareableUrl);
            _mockReceiptService.Verify(s => s.GetShareableLinkAsync(It.IsAny<ReceiptShareRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
