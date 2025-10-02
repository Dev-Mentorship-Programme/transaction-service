using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionService.Domain.Entities
{
    [Table("SignedLinks")]
    public class SignedLink
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ResourceType { get; set; } = "Receipt";

        [Required]
        [MaxLength(500)]
        public string ShareableUrl { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public SignedLink()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public SignedLink(Guid transactionId, string shareableUrl, DateTime expiresAt, string resourceType = "Receipt")
            : this()
        {
            if (transactionId == Guid.Empty)
                throw new ArgumentException("TransactionId cannot be empty", nameof(transactionId));

            if (string.IsNullOrWhiteSpace(shareableUrl))
                throw new ArgumentException("ShareableUrl cannot be empty", nameof(shareableUrl));

            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("ExpiresAt must be in the future", nameof(expiresAt));

            TransactionId = transactionId;
            ShareableUrl = shareableUrl;
            ExpiresAt = expiresAt;
            ResourceType = resourceType;
        }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public void Deactivate()
        {
            IsActive = false;
        }

        public bool IsValid => IsActive && !IsExpired;
    }
}
