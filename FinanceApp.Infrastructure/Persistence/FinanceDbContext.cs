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
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<SharedReport> SharedReports { get; set; } = null!;
    public DbSet<Income> Incomes { get; set; } = null!;
    public DbSet<RecurringTemplate> RecurringTemplates { get; set; } = null!;
    public DbSet<UserFeedback> UserFeedbacks { get; set; } = null!;
    public DbSet<SubscriptionPurchaseRecord> SubscriptionPurchaseRecords { get; set; } = null!;

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

            entity.Property(u => u.SubscriptionBillingSource).HasConversion<int>().IsRequired();
            entity.Property(u => u.AppleOriginalTransactionId).HasMaxLength(128);
            entity.Property(u => u.GooglePurchaseToken).HasMaxLength(2048);
            entity.Property(u => u.PreferredLanguage).HasMaxLength(10).HasDefaultValue("en");
            entity.Property(u => u.DailyReminderEnabled).HasDefaultValue(true);
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
            entity.HasOne(e => e.Account)
                  .WithMany()
                  .HasForeignKey(e => e.AccountId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Transaction>()
                  .WithMany()
                  .HasForeignKey(e => e.TransactionId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.UserId, e.ExpenseDate });
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
            entity.HasIndex(t => new { t.UserId, t.Date });
            entity.HasIndex(t => new { t.UserId, t.Type, t.Date });

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

        // ==============================
        // Notification configuration
        // ==============================
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.Property(n => n.UserId).HasMaxLength(450).IsRequired();
            entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
            entity.Property(n => n.Message).HasMaxLength(1000).IsRequired();
            entity.Property(n => n.RelatedLink).HasMaxLength(500);
            entity.Property(n => n.TopicKey).HasMaxLength(200);
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => new { n.UserId, n.TopicKey }).HasFilter("[TopicKey] IS NOT NULL");
        });

        // ==============================
        // SharedReport configuration
        // ==============================
        modelBuilder.Entity<SharedReport>(entity =>
        {
            entity.ToTable("SharedReports");
            entity.Property(s => s.UserId).HasMaxLength(450).IsRequired();
            entity.Property(s => s.Token).HasMaxLength(64).IsRequired();
            entity.HasIndex(s => s.Token).IsUnique();
            entity.HasIndex(s => s.UserId);
        });

        // ==============================
        // Income configuration
        // ==============================
        modelBuilder.Entity<Income>(entity =>
        {
            entity.ToTable("Incomes");
            entity.Property(i => i.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(i => i.UserId).HasMaxLength(450).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(500);
            entity.Property(i => i.Source).HasMaxLength(200);
            entity.HasIndex(i => i.UserId);
            entity.HasIndex(i => new { i.UserId, i.IncomeDate });
            entity.HasOne(i => i.Account)
                .WithMany()
                .HasForeignKey(i => i.AccountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.Transaction)
                .WithMany()
                .HasForeignKey(i => i.TransactionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ==============================
        // RecurringTemplate configuration
        // ==============================
        modelBuilder.Entity<RecurringTemplate>(entity =>
        {
            entity.ToTable("RecurringTemplates");
            entity.Property(r => r.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(r => r.UserId).HasMaxLength(450).IsRequired();
            entity.Property(r => r.Note).HasMaxLength(500);
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.NextRunDate);
            entity.HasOne(r => r.Account)
                .WithMany()
                .HasForeignKey(r => r.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Category)
                .WithMany()
                .HasForeignKey(r => r.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ==============================
        // UserFeedback configuration
        // ==============================
        modelBuilder.Entity<UserFeedback>(entity =>
        {
            entity.ToTable("UserFeedbacks");
            entity.Property(f => f.UserId).HasMaxLength(450).IsRequired();
            entity.Property(f => f.Subject).HasMaxLength(200);
            entity.Property(f => f.Message).HasMaxLength(4000).IsRequired();
            entity.Property(f => f.AdminNotes).HasMaxLength(2000);
            entity.HasIndex(f => f.UserId);
            entity.HasIndex(f => f.Status);
            entity.HasIndex(f => f.Type);
        });

        modelBuilder.Entity<SubscriptionPurchaseRecord>(entity =>
        {
            entity.ToTable("SubscriptionPurchaseRecords");
            entity.Property(p => p.UserId).HasMaxLength(450).IsRequired();
            entity.Property(p => p.ProductId).HasMaxLength(256).IsRequired();
            entity.Property(p => p.ExternalTransactionId).HasMaxLength(512).IsRequired();
            entity.Property(p => p.Notes).HasMaxLength(2000);
            entity.Property(p => p.BillingSource).HasConversion<int>().IsRequired();
            entity.Property(p => p.Plan).HasConversion<int>().IsRequired();
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => new { p.BillingSource, p.ExternalTransactionId });
        });
    }
}