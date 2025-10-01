using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Interfaces;
using TransactionService.Domain.Entities;
using TransactionService.Domain.ValueObjects;
using TransactionService.Infrastructure.Interfaces;

namespace TransactionService.Infrastructure.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IReceiptGeneratorService _receiptGenerator;
        private readonly ILogger<ReceiptService> _logger;

        public ReceiptService(
            IAppDbContext context,
            ICloudinaryService cloudinaryService,
            IReceiptGeneratorService receiptGenerator,
            ILogger<ReceiptService> logger)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _receiptGenerator = receiptGenerator;
            _logger = logger;
        }

        public async Task<ReceiptDocument> GenerateReceiptAsync(Guid transactionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating receipt document for transaction {TransactionId}", transactionId);

            try
            {
                // Get transaction
                var transaction = _context.Transactions.FirstOrDefault(t => t.Id == transactionId);
                if (transaction == null)
                {
                    throw new InvalidOperationException($"Transaction {transactionId} not found");
                }

                // Check if receipt document already exists
                var existingDocument = _context.ReceiptDocuments
                    .FirstOrDefault(rd => rd.TransactionId == transactionId);

                if (existingDocument != null)
                {
                    _logger.LogInformation("Receipt document already exists for transaction {TransactionId}", transactionId);
                    return existingDocument;
                }

                // Generate PDF
                using var pdfStream = await _receiptGenerator.GenerateReceiptPdfAsync(transaction, cancellationToken);
                
                // Upload to Cloudinary
                var fileName = $"receipt_{transaction.Reference}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                var (documentUrl, publicId) = await _cloudinaryService.UploadDocumentAsync(
                    pdfStream, fileName, "receipts", cancellationToken);

                // Create receipt document entity
                var receiptDocument = new ReceiptDocument(transactionId, documentUrl, publicId);

                _logger.LogInformation("Receipt document generated successfully for transaction {TransactionId}", transactionId);
                return receiptDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt document for transaction {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<SignedLink> GetShareableLinkAsync(ReceiptShareRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating shareable link for transaction {TransactionId} requested by {RequestedBy}", 
                request.TransactionId, request.RequestedBy);

            try
            {
                // Ensure receipt document exists
                var receiptDocument = await GenerateReceiptAsync(request.TransactionId, cancellationToken);

                // Generate secure shareable URL
                var secureUrl = await _cloudinaryService.GenerateSecureUrlAsync(
                    receiptDocument.CloudinaryPublicId, 
                    request.ExpirationHours, 
                    cancellationToken);

                // Create signed link
                var expiresAt = request.CalculateExpirationDate();
                var signedLink = new SignedLink(request.TransactionId, secureUrl, expiresAt, "Receipt");

                _logger.LogInformation("Shareable link created for transaction {TransactionId}, expires at {ExpiresAt}", 
                    request.TransactionId, expiresAt);

                return signedLink;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shareable link for transaction {TransactionId}", request.TransactionId);
                throw;
            }
        }

        public async Task<bool> ValidateLinkAsync(string shareableUrl, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating shareable link: {ShareableUrl}", shareableUrl);

            try
            {
                var signedLink = _context.SignedLinks
                    .FirstOrDefault(sl => sl.ShareableUrl == shareableUrl);

                if (signedLink == null)
                {
                    _logger.LogWarning("Shareable link not found: {ShareableUrl}", shareableUrl);
                    return false;
                }

                var isValid = signedLink.IsValid;
                
                _logger.LogInformation("Link validation result for {ShareableUrl}: {IsValid}", shareableUrl, isValid);
                
                await Task.CompletedTask;
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating shareable link: {ShareableUrl}", shareableUrl);
                return false;
            }
        }
    }
}
