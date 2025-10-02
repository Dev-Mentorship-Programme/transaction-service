using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Interfaces;
using TransactionService.Application.Queries;

namespace TransactionService.Application.Handlers
{
    public class ValidateReceiptLinkQueryHandler : IRequestHandler<ValidateReceiptLinkQuery, bool>
    {
        private readonly IReceiptService _receiptService;
        private readonly ILogger<ValidateReceiptLinkQueryHandler> _logger;

        public ValidateReceiptLinkQueryHandler(
            IReceiptService receiptService,
            ILogger<ValidateReceiptLinkQueryHandler> logger)
        {
            _receiptService = receiptService;
            _logger = logger;
        }

        public async Task<bool> Handle(ValidateReceiptLinkQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating receipt link: {ShareableUrl}", request.ShareableUrl);

            try
            {
                var isValid = await _receiptService.ValidateLinkAsync(request.ShareableUrl, cancellationToken);
                
                _logger.LogInformation("Link validation result for {ShareableUrl}: {IsValid}", 
                    request.ShareableUrl, isValid);
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating receipt link: {ShareableUrl}", request.ShareableUrl);
                return false;
            }
        }
    }
}
