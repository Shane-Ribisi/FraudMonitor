namespace FraudMonitor.Models;

public enum TransactionStatus { Settled, Flagged, Blocked, Declined }
public enum AlertSeverity { Low, Medium, High }

public class Account
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public string HolderName { get; set; } = "";
    public decimal Balance { get; set; }
    public string HomeCountry { get; set; } = "ZA";
}

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public decimal Amount { get; set; }
    public string Merchant { get; set; } = "";
    public string Country { get; set; } = "ZA";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TransactionStatus Status { get; set; }
    public int RiskScore { get; set; }
    public double ProcessingMs { get; set; }
}

public class FraudAlert
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public string Rule { get; set; } = "";
    public AlertSeverity Severity { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Resolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Details { get; set; } = "";
}
