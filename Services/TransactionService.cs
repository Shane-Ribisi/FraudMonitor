using System.Diagnostics;
using FraudMonitor.Data;
using FraudMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudMonitor.Services;

public record TransactionRequest(string AccountNumber, decimal Amount, string Merchant, string Country);

public record TransactionResult(
    int Id, string AccountNumber, decimal Amount, string Merchant, string Country,
    DateTime Timestamp, string Status, int RiskScore, double ProcessingMs, List<string> Alerts);

public class TransactionService
{
    private readonly AppDbContext _db;
    private readonly FraudRuleEngine _rules;
    private readonly AuditService _audit;
    private readonly ILogger<TransactionService> _log;

    public TransactionService(AppDbContext db, FraudRuleEngine rules, AuditService audit, ILogger<TransactionService> log)
    {
        _db = db; _rules = rules; _audit = audit; _log = log;
    }

    public async Task<TransactionResult?> ProcessAsync(TransactionRequest req)
    {
        var sw = Stopwatch.StartNew();
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == req.AccountNumber);
        if (account == null) return null;

        var now = DateTime.UtcNow;
        var tx = new Transaction
        {
            AccountId = account.Id, Amount = req.Amount, Merchant = req.Merchant,
            Country = req.Country.ToUpperInvariant(), Timestamp = now
        };
        var hits = new List<RuleHit>();

        if (req.Amount <= 0 || account.Balance < req.Amount)
        {
            tx.Status = TransactionStatus.Declined;
        }
        else
        {
            // Fraud evaluation happens BEFORE settlement.
            hits = await _rules.EvaluateAsync(account, req.Amount, tx.Country, now);
            tx.RiskScore = FraudRuleEngine.TotalScore(hits);
            tx.Status = FraudRuleEngine.Decide(tx.RiskScore);
            if (tx.Status == TransactionStatus.Settled)
                account.Balance -= req.Amount; // funds only move when the transaction is clean
        }

        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync(); // get tx.Id

        foreach (var h in hits)
        {
            _db.Alerts.Add(new FraudAlert
            {
                TransactionId = tx.Id, Rule = h.Rule, Severity = h.Severity, Description = h.Description
            });
        }

        sw.Stop();
        tx.ProcessingMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);

        _audit.Record($"TRANSACTION_{tx.Status.ToString().ToUpper()}", "Transaction", tx.Id.ToString(),
            $"Account {account.AccountNumber}, R{req.Amount:N2}, {tx.Country}, score {tx.RiskScore}, rules [{string.Join(",", hits.Select(h => h.Rule))}]");
        await _db.SaveChangesAsync();

        if (tx.Status is TransactionStatus.Blocked or TransactionStatus.Flagged)
            _log.LogWarning("Fraud {Status}: tx {Id} account {Acc} score {Score}", tx.Status, tx.Id, account.AccountNumber, tx.RiskScore);
        else
            _log.LogInformation("Transaction {Id} {Status} in {Ms}ms", tx.Id, tx.Status, tx.ProcessingMs);

        return new TransactionResult(tx.Id, account.AccountNumber, tx.Amount, tx.Merchant, tx.Country,
            tx.Timestamp, tx.Status.ToString(), tx.RiskScore, tx.ProcessingMs,
            hits.Select(h => $"{h.Rule}: {h.Description}").ToList());
    }
}
