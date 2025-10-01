using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TransactionService.Domain.Entities;
using TransactionService.Infrastructure.Interfaces;

namespace TransactionService.Infrastructure.Services
{
    public class ReceiptGeneratorService : IReceiptGeneratorService
    {
        private readonly ILogger<ReceiptGeneratorService> _logger;

        public ReceiptGeneratorService(ILogger<ReceiptGeneratorService> logger)
        {
            _logger = logger;
        }

        public async Task<Stream> GenerateReceiptPdfAsync(
            Transaction transaction, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating PDF receipt for transaction {TransactionId}", transaction.Id);

                // Generate HTML content for the receipt
                var htmlContent = GenerateReceiptHtml(transaction);
                
                // Convert HTML to PDF (simplified - in real implementation, use a library like wkhtmltopdf or PuppeteerSharp)
                var pdfBytes = await ConvertHtmlToPdfAsync(htmlContent, cancellationToken);
                
                _logger.LogInformation("PDF receipt generated successfully for transaction {TransactionId}", transaction.Id);
                
                return new MemoryStream(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF receipt for transaction {TransactionId}", transaction.Id);
                throw;
            }
        }

        private string GenerateReceiptHtml(Transaction transaction)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<title>Transaction Receipt</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine(".header { text-align: center; border-bottom: 2px solid #333; padding-bottom: 10px; }");
            html.AppendLine(".content { margin-top: 20px; }");
            html.AppendLine(".field { margin: 10px 0; }");
            html.AppendLine(".label { font-weight: bold; display: inline-block; width: 150px; }");
            html.AppendLine(".amount { font-size: 1.2em; color: #2e7d32; font-weight: bold; }");
            html.AppendLine(".footer { margin-top: 30px; text-align: center; font-size: 0.9em; color: #666; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");
            
            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Transaction Receipt</h1>");
            html.AppendLine("<p>Transaction Service</p>");
            html.AppendLine("</div>");
            
            html.AppendLine("<div class='content'>");
            html.AppendLine($"<div class='field'><span class='label'>Transaction ID:</span> {transaction.Id}</div>");
            html.AppendLine($"<div class='field'><span class='label'>Reference:</span> {transaction.Reference}</div>");
            html.AppendLine($"<div class='field'><span class='label'>Date:</span> {transaction.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</div>");
            html.AppendLine($"<div class='field'><span class='label'>Type:</span> {transaction.Type}</div>");
            html.AppendLine($"<div class='field'><span class='label'>Status:</span> {transaction.Status}</div>");
            html.AppendLine($"<div class='field'><span class='label'>Channel:</span> {transaction.Channel}</div>");
            html.AppendLine($"<div class='field'><span class='label'>Currency:</span> {transaction.Currency}</div>");
            html.AppendLine($"<div class='field amount'><span class='label'>Amount:</span> {transaction.Currency} {transaction.Amount:N2}</div>");
            html.AppendLine($"<div class='field'><span class='label'>Opening Balance:</span> {transaction.Currency} {transaction.OpeningBalance:N2}</div>");
            
            if (transaction.ClosingBalance.HasValue)
            {
                html.AppendLine($"<div class='field'><span class='label'>Closing Balance:</span> {transaction.Currency} {transaction.ClosingBalance.Value:N2}</div>");
            }
            
            html.AppendLine($"<div class='field'><span class='label'>Narration:</span> {transaction.Narration}</div>");
            html.AppendLine($"<div class='field'><span class='label'>Account ID:</span> {transaction.AccountId}</div>");
            html.AppendLine($"<div class='field'><span class='label'>Destination:</span> {transaction.DestinationAccountId}</div>");
            html.AppendLine("</div>");
            
            html.AppendLine("<div class='footer'>");
            html.AppendLine("<p>This is a computer-generated receipt.</p>");
            html.AppendLine($"<p>Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            html.AppendLine("</div>");
            
            html.AppendLine("</body></html>");
            
            return html.ToString();
        }

        private async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, CancellationToken cancellationToken)
        {
            // Simplified PDF generation - in production, use a proper HTML to PDF library
            // For now, create a mock PDF with the HTML content embedded
            _logger.LogInformation("Converting HTML to PDF (mock implementation)");
            
            await Task.Delay(200, cancellationToken); // Simulate conversion time
            
            var pdfHeader = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
            var pdfContent = $"2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /Contents 4 0 R >>\nendobj\n4 0 obj\n<< /Length {htmlContent.Length} >>\nstream\n{htmlContent}\nendstream\nendobj\n";
            var pdfFooter = "xref\n0 5\n0000000000 65535 f \n0000000009 00000 n \n0000000058 00000 n \n0000000115 00000 n \n0000000174 00000 n \ntrailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n" + (pdfHeader.Length + pdfContent.Length) + "\n%%EOF";
            
            var fullPdf = pdfHeader + pdfContent + pdfFooter;
            return Encoding.UTF8.GetBytes(fullPdf);
        }
    }
}
