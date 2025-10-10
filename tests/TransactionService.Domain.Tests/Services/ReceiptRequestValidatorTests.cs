using TransactionService.Domain.Interfaces;
using TransactionService.Domain.Services;
using TransactionService.Domain.ValueObjects;
using Xunit;

namespace TransactionService.Domain.Tests.Services
{
    public class ReceiptRequestValidatorTests
    {
        private readonly ReceiptRequestValidator _validator;

        public ReceiptRequestValidatorTests()
        {
            _validator = new ReceiptRequestValidator();
        }

        [Fact]
        public void Validate_WithValidParameters_ShouldReturnValid()
        {
            // Arrange
            var requestedBy = "user@example.com";
            var expirationHours = 24;

            // Act
            var result = _validator.Validate(requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.True(result.IsValid);
                Assert.Empty(result.Errors);
            });
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WithInvalidRequestedBy_ShouldReturnInvalid(string requestedBy)
        {
            // Arrange
            var expirationHours = 24;

            // Act
            var result = _validator.Validate(requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.False(result.IsValid);
                Assert.Contains("requestedBy parameter is required", result.Errors);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Validate_WithZeroOrNegativeExpirationHours_ShouldReturnInvalid(int expirationHours)
        {
            // Arrange
            var requestedBy = "user@example.com";

            // Act
            var result = _validator.Validate(requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.False(result.IsValid);
                Assert.Contains("expirationHours must be between 1 and 168 hours", result.Errors);
            });
        }

        [Theory]
        [InlineData(169)]
        [InlineData(200)]
        [InlineData(1000)]
        public void Validate_WithExcessiveExpirationHours_ShouldReturnInvalid(int expirationHours)
        {
            // Arrange
            var requestedBy = "user@example.com";

            // Act
            var result = _validator.Validate(requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.False(result.IsValid);
                Assert.Contains("expirationHours must be between 1 and 168 hours", result.Errors);
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(24)]
        [InlineData(72)]
        [InlineData(168)]
        public void Validate_WithValidExpirationHours_ShouldReturnValid(int expirationHours)
        {
            // Arrange
            var requestedBy = "user@example.com";

            // Act
            var result = _validator.Validate(requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.True(result.IsValid);
                Assert.Empty(result.Errors);
            });
        }

        [Fact]
        public void Validate_WithBothInvalidParameters_ShouldReturnMultipleErrors()
        {
            // Arrange
            var requestedBy = "";
            var expirationHours = 0;

            // Act
            var result = _validator.Validate(requestedBy, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.False(result.IsValid);
                Assert.Equal(2, result.Errors.Count);
                Assert.Contains("requestedBy parameter is required", result.Errors);
                Assert.Contains("expirationHours must be between 1 and 168 hours", result.Errors);
            });
        }

        [Fact]
        public void ValidationResult_AddError_ShouldSetIsValidToFalse()
        {
            // Arrange
            var validationResult = new ValidationResult(true);

            // Act
            validationResult.AddError("Test error");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.False(validationResult.IsValid);
                Assert.Contains("Test error", validationResult.Errors);
            });
        }

        [Fact]
        public void ValidationResult_DefaultConstructor_ShouldBeValid()
        {
            // Act
            var validationResult = new ValidationResult();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.True(validationResult.IsValid);
                Assert.Empty(validationResult.Errors);
            });
        }
    }
}
