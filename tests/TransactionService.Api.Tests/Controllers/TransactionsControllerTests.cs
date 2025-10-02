using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TransactionService.Api.Controllers;
using TransactionService.Application.DTOs;
using TransactionService.Application.Queries;
using Xunit;

namespace TransactionService.Api.Tests.Controllers
{
    public class TransactionsControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly TransactionsController _controller;

        public TransactionsControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new TransactionsController(_mockMediator.Object);
        }

        [Fact]
        public async Task GetTransactionReceipt_WithValidParameters_ShouldReturnOk()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 24;
            
            var receiptDto = new ReceiptDto(
                transactionId,
                "https://secure.cloudinary.com/receipt/123",
                DateTime.UtcNow.AddHours(24),
                "https://cloudinary.com/receipt.pdf"
            );

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(receiptDto);

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<ReceiptResponse>();
            
            var response = okResult.Value as ReceiptResponse;
            response!.TransactionId.Should().Be(transactionId);
            response.ShareableUrl.Should().Be("https://secure.cloudinary.com/receipt/123");
        }

        [Fact]
        public async Task GetTransactionReceipt_WithMissingRequestedBy_ShouldReturnBadRequest()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "";
            var expirationHours = 24;

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "requestedBy parameter is required" });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(169)]
        public async Task GetTransactionReceipt_WithInvalidExpirationHours_ShouldReturnBadRequest(int expirationHours)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "expirationHours must be between 1 and 168 hours" });
        }

        [Fact]
        public async Task GetTransactionReceipt_WithNonExistentTransaction_ShouldReturnNotFound()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 24;

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReceiptDto?)null);

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { error = $"Transaction {transactionId} not found" });
        }

        [Fact]
        public async Task GetTransactionReceipt_WithArgumentException_ShouldReturnBadRequest()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 24;

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid transaction ID"));

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid transaction ID" });
        }

        [Fact]
        public async Task GetTransactionReceipt_WithUnexpectedException_ShouldReturnInternalServerError()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 24;

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while generating the receipt" });
        }

        [Fact]
        public async Task ValidateReceiptLink_WithValidUrl_ShouldReturnOk()
        {
            // Arrange
            var url = "https://secure.cloudinary.com/receipt/123";

            _mockMediator.Setup(m => m.Send(It.IsAny<ValidateReceiptLinkQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ValidateReceiptLink(url);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { isValid = true, url });
        }

        [Fact]
        public async Task ValidateReceiptLink_WithInvalidUrl_ShouldReturnOkWithFalse()
        {
            // Arrange
            var url = "https://secure.cloudinary.com/receipt/expired";

            _mockMediator.Setup(m => m.Send(It.IsAny<ValidateReceiptLinkQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ValidateReceiptLink(url);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { isValid = false, url });
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task ValidateReceiptLink_WithMissingUrl_ShouldReturnBadRequest(string url)
        {
            // Act
            var result = await _controller.ValidateReceiptLink(url);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { error = "url parameter is required" });
        }

        [Fact]
        public async Task ValidateReceiptLink_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var url = "https://secure.cloudinary.com/receipt/123";

            _mockMediator.Setup(m => m.Send(It.IsAny<ValidateReceiptLinkQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Service unavailable"));

            // Act
            var result = await _controller.ValidateReceiptLink(url);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeEquivalentTo(new { error = "An error occurred while validating the link" });
        }

        [Fact]
        public async Task GetTransactionReceipt_ShouldCallMediatorWithCorrectQuery()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 48;
            
            var receiptDto = new ReceiptDto(transactionId, "https://test.com", DateTime.UtcNow.AddHours(48));

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(receiptDto);

            // Act
            await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            _mockMediator.Verify(m => m.Send(
                It.Is<GetTransactionReceiptQuery>(q => 
                    q.TransactionId == transactionId &&
                    q.RequestedBy == requestedBy &&
                    q.ExpirationHours == expirationHours),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ValidateReceiptLink_ShouldCallMediatorWithCorrectQuery()
        {
            // Arrange
            var url = "https://secure.cloudinary.com/receipt/123";

            _mockMediator.Setup(m => m.Send(It.IsAny<ValidateReceiptLinkQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _controller.ValidateReceiptLink(url);

            // Assert
            _mockMediator.Verify(m => m.Send(
                It.Is<ValidateReceiptLinkQuery>(q => q.ShareableUrl == url),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
