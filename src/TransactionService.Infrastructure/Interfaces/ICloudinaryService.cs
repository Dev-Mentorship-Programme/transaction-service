using System;
using System.IO;
using System.Threading.Tasks;

namespace TransactionService.Infrastructure.Interfaces
{
    public interface ICloudinaryService
    {
        Task<(string DocumentUrl, string PublicId)> UploadDocumentAsync(
            Stream documentStream, 
            string fileName, 
            string folder = "receipts", 
            CancellationToken cancellationToken = default);

        Task<string> GenerateSecureUrlAsync(
            string publicId, 
            int expirationHours = 24, 
            CancellationToken cancellationToken = default);

        Task<bool> DeleteDocumentAsync(
            string publicId, 
            CancellationToken cancellationToken = default);
    }
}
