using Microsoft.AspNetCore.Mvc;
using MediatR;
using TransactionService.Application.Commands;
using TransactionService.Application.Queries;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Api.Controllers
{
    [ApiController]
    [Route("api/v1/transactions")]
    public class TransactionsController(IMediator mediator, IReceiptRequestValidator receiptRequestValidator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly IReceiptRequestValidator _receiptRequestValidator = receiptRequestValidator;

        [HttpGet("/{id}")]
        public async Task<IActionResult> GetTransaction([FromRoute] Guid id)
        {
            var q = new GetTransactionQuery(id);
            var res = await _mediator.Send(q);
            return res == null ? NotFound() : Ok(res);
        }

        [HttpGet("/{id}/receipt")]
        public async Task<IActionResult> GetTransactionReceipt(
            [FromRoute] Guid id, 
            [FromQuery] string requestedBy,
            [FromQuery] int expirationHours = 24)
        {
            var validationResult = _receiptRequestValidator.Validate(requestedBy, expirationHours);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { errors = validationResult.Errors });
            }

            try
            {
                var query = new GetTransactionReceiptQuery(id, requestedBy, expirationHours);
                var result = await _mediator.Send(query);
                
                if (result == null)
                {
                    return NotFound(new { error = $"Transaction {id} not found" });
                }

                return Ok(new ReceiptResponse(
                    result.TransactionId,
                    result.ShareableUrl,
                    result.ExpiresAt,
                    result.DocumentUrl
                ));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while generating the receipt" });
            }
        }

        [HttpGet("/receipt/validate")]
        public async Task<IActionResult> ValidateReceiptLink([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new { error = "url parameter is required" });
            }

            try
            {
                var query = new ValidateReceiptLinkQuery(url);
                var isValid = await _mediator.Send(query);
                
                return Ok(new { isValid, url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while validating the link" });
            }
        }
    }

}


public record ReceiptResponse(
    Guid TransactionId,
    string ShareableUrl,
    DateTime ExpiresAt,
    string? DocumentUrl = null
);
