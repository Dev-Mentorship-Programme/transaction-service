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
        
        // Implement the interface property
        IQueryable<Transaction> IAppDbContext.Transactions => Transactions;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => base.SaveChangesAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Move your entity configuration here from the deleted class
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
        }
    }
}