using MediatR;

namespace TransactionService.Application.Queries
{
    public record ValidateReceiptLinkQuery(string ShareableUrl) : IRequest<bool>;
}
