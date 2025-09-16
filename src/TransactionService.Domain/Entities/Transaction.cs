using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionService.Domain.Entities
{
    [Table("Transactions")]
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        public Guid DestinationAccountId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ClosingBalance { get; set; }

        [MaxLength(500)]
        public string Narration { get; set; } = "Payment Transaction";

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public TransactionCurrency Currency { get; set; } = TransactionCurrency.NGN;

        public TransactionChannel Channel { get; set; } = TransactionChannel.TRANSFER;

        public TransactionStatus Status { get; set; } = TransactionStatus.PENDING;

        public string? Metadata { get; set; }
        
        private string? _reference;

        [MaxLength(100)]
        public string Reference
        {
            get => _reference ??= $"TR-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            set => _reference = value;
        }

        public Transaction()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
