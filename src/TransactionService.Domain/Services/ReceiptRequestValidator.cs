using TransactionService.Domain.Interfaces;

namespace TransactionService.Domain.Services
{
    public class ReceiptRequestValidator : IReceiptRequestValidator
    {
        public ValidationResult Validate(string requestedBy, int expirationHours)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(requestedBy))
            {
                result.AddError("requestedBy parameter is required");
            }

            if (expirationHours <= 0 || expirationHours > 168)
            {
                result.AddError("expirationHours must be between 1 and 168 hours");
            }

            return result;
        }
    }
}
