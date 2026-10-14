using FraudMonitor.Data;
using FraudMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudMonitor.Services;

public record RuleHit(string Rule, AlertSeverity Severity, int Score, string Description);

public class FraudRuleEngine
{
    public const int BlockThreshold = 70;
    public const int FlagThreshold = 40;

    private readonly AppDbContext _db;
    public FraudRuleEngine(AppDbContext db) => _db = db;

    public async Task<List<RuleHit>> EvaluateAsync(Account account, decimal amount, string country, DateTime now)
    {
        var hits = new List<RuleHit>();

        // Rule 1: large amount
        if (amount >= 50_000m)
            hits.Add(new("LARGE_AMOUNT", AlertSeverity.High, 80, $"Amount R{amount:N2} exceeds R50,000 limit"));
        else if (amount >= 20_000m)
            hits.Add(new("LARGE_AMOUNT", AlertSeverity.Medium, 45, $"Amount R{amount:N2} exceeds R20,000 review limit"));

        // Rule 2: high-velocity micro-transactions (5+ in the last 60 seconds)
        var since = now.AddSeconds(-60);
        var recentCount = await _db.Transactions
            .CountAsync(t => t.AccountId == account.Id && t.Timestamp >= since);
        if (recentCount >= 5)
            hits.Add(new("HIGH_VELOCITY", AlertSeverity.High, 75,
                $"{recentCount + 1} transactions within 60 seconds on account {account.AccountNumber}"));

        // Rule 3: location anomalies
        if (!string.Equals(country, account.HomeCountry, StringComparison.OrdinalIgnoreCase))
        {
            hits.Add(new("FOREIGN_LOCATION", AlertSeverity.Medium, 50,
                $"Transaction from {country}, account home country is {account.HomeCountry}"));
        }

        var windowStart = now.AddMinutes(-10);
        var last = await _db.Transactions
            .Where(t => t.AccountId == account.Id && t.Timestamp >= windowStart)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync();
        if (last != null && !string.Equals(last.Country, country, StringComparison.OrdinalIgnoreCase))
        {
            hits.Add(new("IMPOSSIBLE_TRAVEL", AlertSeverity.High, 80,
                $"Country changed from {last.Country} to {country} within 10 minutes"));
        }

        return hits;
    }

    public static int TotalScore(IEnumerable<RuleHit> hits) => Math.Min(100, hits.Sum(h => h.Score));

    public static TransactionStatus Decide(int score) =>
        score >= BlockThreshold ? TransactionStatus.Blocked
        : score >= FlagThreshold ? TransactionStatus.Flagged
        : TransactionStatus.Settled;
}
