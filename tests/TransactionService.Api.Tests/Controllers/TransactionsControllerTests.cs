using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TransactionService.Api.Controllers;
using TransactionService.Application.DTOs;
using TransactionService.Application.Queries;
using TransactionService.Domain.Interfaces;
using TransactionService.Domain.ValueObjects;
using Xunit;

namespace TransactionService.Api.Tests.Controllers
{
    public class TransactionsControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<IReceiptRequestValidator> _mockValidator;
        private readonly TransactionsController _controller;

        public TransactionsControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _mockValidator = new Mock<IReceiptRequestValidator>();
            _controller = new TransactionsController(_mockMediator.Object, _mockValidator.Object);
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

            _mockValidator.Setup(v => v.Validate(requestedBy, expirationHours))
                .Returns(new ValidationResult(true));

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(receiptDto);

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsType<OkObjectResult>(result);
                var okResult = result as OkObjectResult;
                Assert.IsType<ReceiptResponse>(okResult!.Value);
                
                var response = okResult.Value as ReceiptResponse;
                Assert.Equal(transactionId, response!.TransactionId);
                Assert.Equal("https://secure.cloudinary.com/receipt/123", response.ShareableUrl);
            });
        }

        [Fact]
        public async Task GetTransactionReceipt_WithMissingRequestedBy_ShouldReturnBadRequest()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "";
            var expirationHours = 24;

            var validationResult = new ValidationResult(false);
            validationResult.AddError("requestedBy parameter is required");
            
            _mockValidator.Setup(v => v.Validate(requestedBy, expirationHours))
                .Returns(validationResult);

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsType<BadRequestObjectResult>(result);
                var badRequestResult = result as BadRequestObjectResult;
                Assert.Equivalent(new { errors = new List<string> { "requestedBy parameter is required" } }, badRequestResult!.Value);
            });
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

            var validationResult = new ValidationResult(false);
            validationResult.AddError("expirationHours must be between 1 and 168 (7 days)");
            
            _mockValidator.Setup(v => v.Validate(requestedBy, expirationHours))
                .Returns(validationResult);

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsType<BadRequestObjectResult>(result);
                var badRequestResult = result as BadRequestObjectResult;
                Assert.Equivalent(new { errors = new List<string> { "expirationHours must be between 1 and 168 (7 days)" } }, badRequestResult!.Value);
            });
        }

        [Fact]
        public async Task GetTransactionReceipt_WithNonExistentTransaction_ShouldReturnNotFound()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 24;

            _mockValidator.Setup(v => v.Validate(requestedBy, expirationHours))
                .Returns(new ValidationResult(true));

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReceiptDto?)null);

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsType<NotFoundObjectResult>(result);
                var notFoundResult = result as NotFoundObjectResult;
                Assert.Equivalent(new { error = $"Transaction {transactionId} not found" }, notFoundResult!.Value);
            });
        }

        [Fact]
        public async Task GetTransactionReceipt_WithArgumentException_ShouldReturnBadRequest()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 24;

            _mockValidator.Setup(v => v.Validate(requestedBy, expirationHours))
                .Returns(new ValidationResult(true));

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid transaction ID"));

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsType<BadRequestObjectResult>(result);
                var badRequestResult = result as BadRequestObjectResult;
                Assert.Equivalent(new { error = "Invalid transaction ID" }, badRequestResult!.Value);
            });
        }

        [Fact]
        public async Task GetTransactionReceipt_WithUnexpectedException_ShouldReturnInternalServerError()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 24;

            _mockValidator.Setup(v => v.Validate(requestedBy, expirationHours))
                .Returns(new ValidationResult(true));

            _mockMediator.Setup(m => m.Send(It.IsAny<GetTransactionReceiptQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act
            var result = await _controller.GetTransactionReceipt(transactionId, requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsType<ObjectResult>(result);
                var objectResult = result as ObjectResult;
                Assert.Equal(500, objectResult!.StatusCode);
                Assert.Equivalent(new { error = "An error occurred while generating the receipt" }, objectResult.Value);
            });
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
            Assert.Multiple(() =>
            {
                Assert.IsType<OkObjectResult>(result);
                var okResult = result as OkObjectResult;
                Assert.Equivalent(new { isValid = true, url }, okResult!.Value);
            });
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
            Assert.Multiple(() =>
            {
                Assert.IsType<OkObjectResult>(result);
                var okResult = result as OkObjectResult;
                Assert.Equivalent(new { isValid = false, url }, okResult!.Value);
            });
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
            Assert.Multiple(() =>
            {
                Assert.IsType<BadRequestObjectResult>(result);
                var badRequestResult = result as BadRequestObjectResult;
                Assert.Equivalent(new { error = "url parameter is required" }, badRequestResult!.Value);
            });
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
            Assert.Multiple(() =>
            {
                Assert.IsType<ObjectResult>(result);
                var objectResult = result as ObjectResult;
                Assert.Equal(500, objectResult!.StatusCode);
                Assert.Equivalent(new { error = "An error occurred while validating the link" }, objectResult.Value);
            });
        }

        [Fact]
        public async Task GetTransactionReceipt_ShouldCallMediatorWithCorrectQuery()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var requestedBy = "user@example.com";
            var expirationHours = 48;
            
            _mockValidator.Setup(v => v.Validate(requestedBy, expirationHours))
                .Returns(new ValidationResult(true));
            
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
