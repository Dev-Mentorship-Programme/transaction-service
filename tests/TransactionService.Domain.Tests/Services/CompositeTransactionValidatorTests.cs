using System.Collections.Generic;
using Xunit;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;
using TransactionService.Domain.Services;

namespace TransactionService.Domain.Tests.Services
{
    public class CompositeTransactionValidatorTests
    {
        [Fact]
        public void CompositeValidator_ShouldPass_WhenAllValidatorsPass()
        {
            // Arrange
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.NGN,
                Amount = 50000
            };

            var validators = new List<ITransactionValidator>
            {
                new DomesticTransactionValidator()
            };

            var compositeValidator = new CompositeTransactionValidator(validators);

            // Act
            var result = compositeValidator.Validate(transaction);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CompositeValidator_ShouldFail_WhenAnyValidatorFails()
        {
            // Arrange
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.NGN,
                Amount = 150000 // Exceeds domestic limit
            };

            var validators = new List<ITransactionValidator>
            {
                new DomesticTransactionValidator(),
                new InternationalTransactionValidator() // Will also fail due to NGN
            };

            var compositeValidator = new CompositeTransactionValidator(validators);

            // Act
            var result = compositeValidator.Validate(transaction);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CompositeValidator_ShouldPass_ForValidInternationalTransaction()
        {
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.USD,
                Amount = 3000
            };

            var validators = new List<ITransactionValidator>
            {
                new InternationalTransactionValidator()
            };

            var compositeValidator = new CompositeTransactionValidator(validators);

            var result = compositeValidator.Validate(transaction);

            Assert.True(result);
        }
    }
}
