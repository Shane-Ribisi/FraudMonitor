using FraudMonitor.Data;
using FraudMonitor.Models;

namespace FraudMonitor.Services;

public class AuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) => _db = db;

    // Adds to the change tracker; persisted by the caller's SaveChanges so audit + business data commit together.
    public void Record(string action, string entity, string entityId, string details) =>
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action, Entity = entity, EntityId = entityId, Details = details
        });
}
