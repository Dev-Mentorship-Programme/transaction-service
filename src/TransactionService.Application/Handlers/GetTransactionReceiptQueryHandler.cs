using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.DTOs;
using TransactionService.Application.Interfaces;
using TransactionService.Application.Queries;
using TransactionService.Domain.ValueObjects;

namespace TransactionService.Application.Handlers
{
    public class GetTransactionReceiptQueryHandler : IRequestHandler<GetTransactionReceiptQuery, ReceiptDto?>
    {
        private readonly IAppDbContext _context;
        private readonly IReceiptService _receiptService;
        private readonly ILogger<GetTransactionReceiptQueryHandler> _logger;

        public GetTransactionReceiptQueryHandler(
            IAppDbContext context,
            IReceiptService receiptService,
            ILogger<GetTransactionReceiptQueryHandler> logger)
        {
            _context = context;
            _receiptService = receiptService;
            _logger = logger;
        }

        public async Task<ReceiptDto?> Handle(GetTransactionReceiptQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing receipt request for transaction {TransactionId} by {RequestedBy}", 
                request.TransactionId, request.RequestedBy);

            // Validate transaction exists
            var transaction = _context.Transactions
                .FirstOrDefault(t => t.Id == request.TransactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found", request.TransactionId);
                return null;
            }

            // Check for existing valid receipt link
            var existingLink = _context.SignedLinks
                .Where(sl => sl.TransactionId == request.TransactionId && 
                           sl.ResourceType == "Receipt" && 
                           sl.IsActive && 
                           sl.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(sl => sl.CreatedAt)
                .FirstOrDefault();

            if (existingLink != null)
            {
                _logger.LogInformation("Found existing valid receipt link for transaction {TransactionId}", request.TransactionId);
                
                var existingDocument = _context.ReceiptDocuments
                    .FirstOrDefault(rd => rd.TransactionId == request.TransactionId);

                return new ReceiptDto(
                    request.TransactionId,
                    existingLink.ShareableUrl,
                    existingLink.ExpiresAt,
                    existingDocument?.DocumentUrl
                );
            }

            // Generate new receipt if needed
            try
            {
                var receiptRequest = new ReceiptShareRequest(
                    request.TransactionId, 
                    request.ExpirationHours, 
                    request.RequestedBy);

                var signedLink = await _receiptService.GetShareableLinkAsync(receiptRequest, cancellationToken);
                
                var receiptDocument = _context.ReceiptDocuments
                    .FirstOrDefault(rd => rd.TransactionId == request.TransactionId);

                _logger.LogInformation("Generated new receipt link for transaction {TransactionId}", request.TransactionId);

                return new ReceiptDto(
                    request.TransactionId,
                    signedLink.ShareableUrl,
                    signedLink.ExpiresAt,
                    receiptDocument?.DocumentUrl
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt for transaction {TransactionId}", request.TransactionId);
                throw;
            }
        }
    }
}
