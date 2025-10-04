using System.Collections.Generic;

namespace TransactionService.Domain.Interfaces
{
    public interface IReceiptRequestValidator
    {
        ValidationResult Validate(string requestedBy, int expirationHours);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();

        public ValidationResult(bool isValid = true)
        {
            IsValid = isValid;
        }

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }
    }
}
