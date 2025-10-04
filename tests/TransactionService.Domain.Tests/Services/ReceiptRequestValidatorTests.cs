using FluentAssertions;
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
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
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
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("requestedBy parameter is required");
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
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("expirationHours must be between 1 and 168 hours");
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
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("expirationHours must be between 1 and 168 hours");
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
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
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
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain("requestedBy parameter is required");
            result.Errors.Should().Contain("expirationHours must be between 1 and 168 hours");
        }

        [Fact]
        public void ValidationResult_AddError_ShouldSetIsValidToFalse()
        {
            // Arrange
            var validationResult = new ValidationResult(true);

            // Act
            validationResult.AddError("Test error");

            // Assert
            validationResult.IsValid.Should().BeFalse();
            validationResult.Errors.Should().Contain("Test error");
        }

        [Fact]
        public void ValidationResult_DefaultConstructor_ShouldBeValid()
        {
            // Act
            var validationResult = new ValidationResult();

            // Assert
            validationResult.IsValid.Should().BeTrue();
            validationResult.Errors.Should().BeEmpty();
        }
    }
}
