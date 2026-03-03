using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity; // <-- where ApplicationUser lives
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence;

public class FinanceDbContext 
    : IdentityDbContext<ApplicationUser> // 🔥 changed here
{
    private readonly DbContextOptions<FinanceDbContext> _options;

    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options)
    {
        _options = options;
    }

    // ==============================
    // DbSets
    // ==============================

    public DbSet<Expense> Expenses { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;
    public DbSet<CategoryBudget> CategoryBudgets { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<SupportingDocument> SupportingDocuments { get; set; } = null!;

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

        // SQLite: fix IdentityPasskeyData key (required for EnsureCreated) and DateTimeOffset ORDER BY
        var isSqlite = _options.Extensions.Any(e => e.GetType().Name == "SqliteOptionsExtension");
        if (isSqlite)
        {
            var passkeyType = modelBuilder.Model.GetEntityTypes().FirstOrDefault(t => t.ClrType.Name == "IdentityPasskeyData");
            if (passkeyType != null)
                modelBuilder.Entity(passkeyType.ClrType).HasNoKey();

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.Namespace?.StartsWith("FinanceApp.", StringComparison.Ordinal) != true)
                    continue;
                foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(DateTimeOffset)))
                    modelBuilder.Entity(entityType.ClrType).Property(property.Name).HasConversion<string>();
            }
        }

        // ==============================
        // Identity user extensions
        // ==============================
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Country).HasMaxLength(100);
            entity.Property(u => u.CountryCode).HasMaxLength(10);
            entity.Property(u => u.SubscriptionPlan)
                  .HasDefaultValue(SubscriptionPlan.Free)
                  .IsRequired();

            entity.Property(u => u.SubscriptionAssignedAt)
                  .HasDefaultValueSql("GETUTCDATE()")
                  .IsRequired();
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

            entity.Property(c => c.Type)
                  .HasConversion<int>()
                  .HasDefaultValue(FinanceApp.Domain.Enums.CategoryType.Expense);
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

        // ==============================
        // CategoryBudget configuration
        // ==============================
        modelBuilder.Entity<CategoryBudget>(entity =>
        {
            entity.ToTable("CategoryBudgets");
            entity.Property(cb => cb.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(cb => cb.UserId).IsRequired();
            entity.HasIndex(cb => new { cb.UserId, cb.CategoryId, cb.Month, cb.Year }).IsUnique();
            entity.HasOne(cb => cb.Category)
                .WithMany()
                .HasForeignKey(cb => cb.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==============================
        // RefreshToken configuration
        // ==============================
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Token).HasMaxLength(500).IsRequired();
            entity.Property(rt => rt.UserId).IsRequired();
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.HasIndex(rt => rt.UserId);
        });

        // ==============================
        // Account configuration
        // ==============================
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");
            entity.Property(a => a.Name).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(500);
            entity.Property(a => a.InitialBalance).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(a => a.UserId).IsRequired();
            entity.HasIndex(a => a.UserId);
        });

        // ==============================
        // Transaction configuration
        // ==============================
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(t => t.UserId).IsRequired();
            entity.Property(t => t.Note).HasMaxLength(500);
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.TransferGroupId);

            entity.HasOne(t => t.Account)
                  .WithMany(a => a.Transactions)
                  .HasForeignKey(t => t.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Category)
                  .WithMany()
                  .HasForeignKey(t => t.CategoryId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ==============================
        // SupportingDocument configuration
        // ==============================
        modelBuilder.Entity<SupportingDocument>(entity =>
        {
            entity.ToTable("SupportingDocuments");
            entity.Property(d => d.UserId).IsRequired();
            entity.Property(d => d.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(d => d.StoredFileName).HasMaxLength(260).IsRequired();
            entity.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(d => d.Label).HasMaxLength(200);
            entity.HasIndex(d => d.UserId);
            entity.HasIndex(d => new { d.EntityType, d.EntityId });
        });
    }
}