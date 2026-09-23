using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Entities;

namespace ShopsManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<DailyActivity> DailyActivities => Set<DailyActivity>();
    public DbSet<ExpenseItem> ExpenseItems => Set<ExpenseItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceBranchAllocation> InvoiceBranchAllocations => Set<InvoiceBranchAllocation>();
    public DbSet<Trader> Traders => Set<Trader>();
    public DbSet<TraderTransaction> TraderTransactions => Set<TraderTransaction>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeTransaction> EmployeeTransactions => Set<EmployeeTransaction>();
    public DbSet<ZakatTable> ZakatTables => Set<ZakatTable>();
    public DbSet<ZakatEntry> ZakatEntries => Set<ZakatEntry>();
    public DbSet<BackupLog> BackupLogs => Set<BackupLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // 1. Branch Configuration
        // ==========================================
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.RecipientName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.OpeningBalance).HasPrecision(18, 2);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // ==========================================
        // 2. DailyActivity Configuration
        // ==========================================
        modelBuilder.Entity<DailyActivity>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint: one activity record per branch per day
            entity.HasIndex(e => new { e.BranchId, e.Date }).IsUnique();

            entity.Property(e => e.TotalSales).HasPrecision(18, 2);
            entity.Property(e => e.TotalExpenses).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);

            // NetSales is computed in code, don't map as column
            entity.Ignore(e => e.NetSales);

            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.DailyActivities)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // 3. ExpenseItem Configuration
        // ==========================================
        modelBuilder.Entity<ExpenseItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.DailyActivity)
                  .WithMany(d => d.ExpenseItems)
                  .HasForeignKey(e => e.DailyActivityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // 4. Invoice Configuration
        // ==========================================
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.Invoices)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);

            entity.HasOne(e => e.Trader)
                  .WithMany(t => t.Invoices)
                  .HasForeignKey(e => e.TraderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceBranchAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.BranchAllocations)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Branch)
                  .WithMany()
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // 5. Trader Configuration
        // ==========================================
        modelBuilder.Entity<Trader>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Location).HasMaxLength(250);
            entity.Property(e => e.BusinessName).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.OpeningBalance).HasPrecision(18, 2);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // ==========================================
        // 6. TraderTransaction Configuration
        // ==========================================
        modelBuilder.Entity<TraderTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Trader)
                  .WithMany(t => t.Transactions)
                  .HasForeignKey(e => e.TraderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RelatedInvoice)
                  .WithMany(i => i.RelatedTraderTransactions)
                  .HasForeignKey(e => e.RelatedInvoiceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ==========================================
        // 7. Employee Configuration
        // ==========================================
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.Salary).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.Employees)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // 8. EmployeeTransaction Configuration
        // ==========================================
        modelBuilder.Entity<EmployeeTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Employee)
                  .WithMany(e => e.Transactions)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // 9. Zakat Configuration
        // ==========================================
        modelBuilder.Entity<ZakatTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        modelBuilder.Entity<ZakatEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ZakatType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.ZakatTable)
                  .WithMany(t => t.Entries)
                  .HasForeignKey(e => e.ZakatTableId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired(false);
        });

        // ==========================================
        // 10. BackupLog Configuration
        // ==========================================
        modelBuilder.Entity<BackupLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.FilePathOrDriveFileId).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });
    }
}
