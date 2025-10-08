using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransactionService.Application.Interfaces;
using TransactionService.Domain.Entities;

namespace TransactionService.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
    {
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<SignedLink> SignedLinks { get; set; } = null!;
        public DbSet<ReceiptDocument> ReceiptDocuments { get; set; } = null!;
        
        IQueryable<Transaction> IAppDbContext.Transactions => Transactions;
        IQueryable<SignedLink> IAppDbContext.SignedLinks => SignedLinks;
        IQueryable<ReceiptDocument> IAppDbContext.ReceiptDocuments => ReceiptDocuments;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => base.SaveChangesAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedNever();

                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.OpeningBalance)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.ClosingBalance)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(e => e.Currency)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasDefaultValue(TransactionCurrency.NGN);

                entity.Property(e => e.Channel)
                    .HasConversion<string>()
                    .HasDefaultValue(TransactionChannel.TRANSFER);

                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasDefaultValue(TransactionStatus.PENDING);

                entity.Property(e => e.Narration)
                    .HasMaxLength(500)
                    .HasDefaultValue("Payment Transaction");

                entity.Property(e => e.Metadata)
                    .HasColumnType("text");

                entity.Property(e => e.Reference)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt);

                // Indexes
                entity.HasIndex(e => e.AccountId)
                    .HasDatabaseName("IX_Transactions_AccountId");

                entity.HasIndex(e => e.DestinationAccountId)
                    .HasDatabaseName("IX_Transactions_DestinationAccountId");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Transactions_Status");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Transactions_CreatedAt");

                entity.HasIndex(e => e.Reference)
                    .HasDatabaseName("IX_Transactions_Reference")
                    .IsUnique();

                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_Transactions_Type");
            });

            modelBuilder.Entity<SignedLink>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedNever();

                entity.Property(e => e.ResourceId)
                    .IsRequired();

                entity.Property(e => e.ResourceType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.ShareableUrl)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.ExpiresAt)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                // Indexes
                entity.HasIndex(e => e.ResourceId)
                    .HasDatabaseName("IX_SignedLinks_TransactionId");

                entity.HasIndex(e => e.ExpiresAt)
                    .HasDatabaseName("IX_SignedLinks_ExpiresAt");

                entity.HasIndex(e => e.ShareableUrl)
                    .HasDatabaseName("IX_SignedLinks_ShareableUrl")
                    .IsUnique();
            });

            modelBuilder.Entity<ReceiptDocument>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedNever();

                entity.Property(e => e.TransactionId)
                    .IsRequired();

                entity.Property(e => e.DocumentUrl)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.CloudinaryPublicId)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Indexes
                entity.HasIndex(e => e.TransactionId)
                    .HasDatabaseName("IX_ReceiptDocuments_TransactionId");
            });
        }
    }
}