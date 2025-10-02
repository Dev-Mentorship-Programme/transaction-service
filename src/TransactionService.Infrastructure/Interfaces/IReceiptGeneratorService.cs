using System.IO;
using System.Threading.Tasks;
using TransactionService.Domain.Entities;

namespace TransactionService.Infrastructure.Interfaces
{
    public interface IReceiptGeneratorService
    {
        Task<Stream> GenerateReceiptPdfAsync(Transaction transaction, CancellationToken cancellationToken = default);
    }
}
