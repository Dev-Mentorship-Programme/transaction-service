using System.Text;

namespace TransactionService.Infrastructure.DocGen;
public interface IDocumentGenerator
{
    Task<byte[]> GenerateReceiptAsync(Guid resourceId, string format = "pdf");
    Task<byte[]> GenerateStatementAsync(IEnumerable<object> transactions, string format = "pdf");
}

public class PdfDocumentGenerator: IDocumentGenerator
{
    public Task<byte[]> GenerateReceiptAsync(Guid resourceId, string format = "pdf")
    {
        // Minimal stub: generate a tiny PDF-like byte array or simple PDF via text. Replace with QuestPDF/Puppeteer in prod
        var text = $"Receipt for transaction {resourceId}\nGeneratedAt: {DateTime.UtcNow:O}";
        var bytes = Encoding.UTF8.GetBytes(text);
        return Task.FromResult(bytes);
    }

    public Task<byte[]> GenerateStatementAsync(IEnumerable<object> transactions, string format = "pdf")
    {
        var text = "Statement\n" + string.Join('\n', transactions.Select(t => t?.ToString()));
        var bytes = Encoding.UTF8.GetBytes(text);
        return Task.FromResult(bytes);
    }
}
