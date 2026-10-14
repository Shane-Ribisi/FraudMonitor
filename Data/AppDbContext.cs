using FraudMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudMonitor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FraudAlert> Alerts => Set<FraudAlert>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>().HasIndex(a => a.AccountNumber).IsUnique();
        b.Entity<Transaction>().HasIndex(t => new { t.AccountId, t.Timestamp });
    }

    // Audit logs are append-only: any attempt to update or delete one is rejected.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceAppendOnlyAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        EnforceAppendOnlyAudit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void EnforceAppendOnlyAudit()
    {
        var illegal = ChangeTracker.Entries<AuditLog>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);
        if (illegal)
            throw new InvalidOperationException("Audit logs are append-only and cannot be modified or deleted.");
    }
}

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Accounts.Any()) return;
        db.Accounts.AddRange(
            new Account { AccountNumber = "1001001", HolderName = "Thabo Mokoena", Balance = 5_000_000m },
            new Account { AccountNumber = "1001002", HolderName = "Aisha Patel", Balance = 5_000_000m },
            new Account { AccountNumber = "1001003", HolderName = "Pieter van der Merwe", Balance = 5_000_000m },
            new Account { AccountNumber = "1001004", HolderName = "Lerato Dlamini", Balance = 5_000_000m },
            new Account { AccountNumber = "1001005", HolderName = "Sipho Nkosi", Balance = 5_000_000m });
        db.SaveChanges();
    }
}
