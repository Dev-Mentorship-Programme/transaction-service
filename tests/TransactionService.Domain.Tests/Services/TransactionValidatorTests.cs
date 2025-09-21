using System;
using Xunit;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Services;

namespace TransactionService.Domain.Tests.Services
{
    public class TransactionValidatorTests
    {
        [Fact]
        public void InternationalValidator_ShouldPass_ForValidInternationalTransaction()
        {
            // Arrange
            var validator = new InternationalTransactionValidator();
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.USD,
                Amount = 5000
            };

            // Act
            var result = validator.Validate(transaction);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void InternationalValidator_ShouldFail_ForNGNTransaction()
        {
            var validator = new InternationalTransactionValidator();
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.NGN,
                Amount = 5000
            };

            var result = validator.Validate(transaction);

            Assert.False(result);
        }

        [Fact]
        public void InternationalValidator_ShouldFail_ForAmountAboveLimit()
        {
            var validator = new InternationalTransactionValidator();
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.USD,
                Amount = 6000
            };

            var result = validator.Validate(transaction);

            Assert.False(result);
        }

        [Fact]
        public void DomesticValidator_ShouldPass_ForValidDomesticTransaction()
        {
            var validator = new DomesticTransactionValidator();
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.NGN,
                Amount = 100000
            };

            var result = validator.Validate(transaction);

            Assert.True(result);
        }

        [Fact]
        public void DomesticValidator_ShouldFail_ForNonNGNTransaction()
        {
            var validator = new DomesticTransactionValidator();
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.USD,
                Amount = 100000
            };

            var result = validator.Validate(transaction);

            Assert.False(result);
        }

        [Fact]
        public void DomesticValidator_ShouldFail_ForAmountAboveLimit()
        {
            var validator = new DomesticTransactionValidator();
            var transaction = new Transaction
            {
                Currency = TransactionCurrency.NGN,
                Amount = 150000
            };

            var result = validator.Validate(transaction);

            Assert.False(result);
        }
    }
}
