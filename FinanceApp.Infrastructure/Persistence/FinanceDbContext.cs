using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Common;

using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Category> Categories { get; set; }

    // Automatically handle CreatedAt, UpdatedAt
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreated(DateTimeOffset.UtcNow);
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkAsUpdated(DateTimeOffset.UtcNow);
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==============================
        // BaseEntity configuration
        // ==============================
        modelBuilder.Entity<BaseEntity>()
            .Property(b => b.Id)
            .ValueGeneratedOnAdd();

        // ==============================
        // Expense configuration
        // ==============================
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Money precision
            entity.Property(e => e.Amount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasMaxLength(500);

            entity.Property(e => e.ReceiptPath)
                  .HasMaxLength(500);

            // Relationships
            entity.HasOne<Category>()
                  .WithMany(c => c.Expenses)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Soft delete global filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ==============================
        // Category configuration
        // ==============================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(c => c.Description)
                  .HasMaxLength(500);

            // Soft delete global filter
            entity.HasQueryFilter(c => !c.IsDeleted);
        });
    }
}