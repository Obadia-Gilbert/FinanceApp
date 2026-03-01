using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Common;
using FinanceApp.Infrastructure.Identity; // <-- where ApplicationUser lives
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence;

public class FinanceDbContext 
    : IdentityDbContext<ApplicationUser> // 🔥 changed here
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options)
    {
    }

    // ==============================
    // DbSets
    // ==============================

    public DbSet<Expense> Expenses { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;

    // ==============================
    // Automatically handle CreatedAt and UpdatedAt
    // ==============================

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
        // 🔥 VERY IMPORTANT: call base FIRST for Identity tables
        base.OnModelCreating(modelBuilder);

        // ==============================
        // BaseEntity configuration
        // ==============================
        modelBuilder.Entity<BaseEntity>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
        });

        // ==============================
        // Category configuration
        // ==============================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.Property(c => c.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(c => c.Description)
                  .HasMaxLength(500);
        });

        // ==============================
        // Expense configuration
        // ==============================
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expenses");

            entity.Property(e => e.Amount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasMaxLength(500);

            entity.Property(e => e.ReceiptPath)
                  .HasMaxLength(500);

            entity.Property(e => e.UserId)
                  .IsRequired(); // 🔥 we will add this in Expense

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Expenses)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // Budget configuration
        // ==============================
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("Budgets");
            entity.Property(b => b.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(b => b.UserId).IsRequired();
            entity.HasIndex(b => new { b.UserId, b.Month, b.Year }).IsUnique();
        });
    }
}