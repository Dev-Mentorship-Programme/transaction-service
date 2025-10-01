using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionService.Domain.Entities
{
    [Table("ReceiptDocuments")]
    public class ReceiptDocument
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        [MaxLength(500)]
        public string DocumentUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string CloudinaryPublicId { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

        public ReceiptDocument()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public ReceiptDocument(Guid transactionId, string documentUrl, string cloudinaryPublicId)
            : this()
        {
            if (transactionId == Guid.Empty)
                throw new ArgumentException("TransactionId cannot be empty", nameof(transactionId));

            if (string.IsNullOrWhiteSpace(documentUrl))
                throw new ArgumentException("DocumentUrl cannot be empty", nameof(documentUrl));

            if (string.IsNullOrWhiteSpace(cloudinaryPublicId))
                throw new ArgumentException("CloudinaryPublicId cannot be empty", nameof(cloudinaryPublicId));

            TransactionId = transactionId;
            DocumentUrl = documentUrl;
            CloudinaryPublicId = cloudinaryPublicId;
        }
    }
}
