using System;

namespace TransactionService.Application.DTOs
{
    public record ReceiptDto(
        Guid TransactionId,
        string ShareableUrl,
        DateTime ExpiresAt,
        string? DocumentUrl = null
    );
}
