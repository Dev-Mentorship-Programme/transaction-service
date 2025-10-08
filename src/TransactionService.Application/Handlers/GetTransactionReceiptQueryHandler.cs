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
        private readonly ITransactionRepository _transactionRepository;
        private readonly ISignedLinkRepository _signedLinkRepository;
        private readonly IReceiptDocumentRepository _receiptDocumentRepository;
        private readonly IReceiptService _receiptService;
        private readonly ILogger<GetTransactionReceiptQueryHandler> _logger;

        public GetTransactionReceiptQueryHandler(
            ITransactionRepository transactionRepository,
            ISignedLinkRepository signedLinkRepository,
            IReceiptDocumentRepository receiptDocumentRepository,
            IReceiptService receiptService,
            ILogger<GetTransactionReceiptQueryHandler> logger)
        {
            _transactionRepository = transactionRepository;
            _signedLinkRepository = signedLinkRepository;
            _receiptDocumentRepository = receiptDocumentRepository;
            _receiptService = receiptService;
            _logger = logger;
        }

        public async Task<ReceiptDto?> Handle(GetTransactionReceiptQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing receipt request for transaction {TransactionId} by {RequestedBy}", 
                request.TransactionId, request.RequestedBy);

            // Validate transaction exists
            var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found", request.TransactionId);
                return null;
            }

            // Check for existing valid receipt link
            var existingLink = await _signedLinkRepository.GetActiveByTransactionIdAsync(
                request.TransactionId, 
                "Receipt", 
                cancellationToken);

            if (existingLink != null)
            {
                _logger.LogInformation("Found existing valid receipt link for transaction {TransactionId}", request.TransactionId);
                
                var existingDocument = await _receiptDocumentRepository.GetByTransactionIdAsync(
                    request.TransactionId, 
                    cancellationToken);

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
                
                var receiptDocument = await _receiptDocumentRepository.GetByTransactionIdAsync(
                    request.TransactionId, 
                    cancellationToken);

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
