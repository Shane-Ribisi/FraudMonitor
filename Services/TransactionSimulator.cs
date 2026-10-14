namespace FraudMonitor.Services;

// Background service that feeds the system with realistic and suspicious traffic.
public class TransactionSimulator : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<TransactionSimulator> _log;
    private readonly Random _rng = new();

    private static readonly string[] Accounts = { "1001001", "1001002", "1001003", "1001004", "1001005" };
    private static readonly string[] Merchants = { "Woolworths", "Checkers", "Engen", "Takealot", "Uber", "Netflix", "Pick n Pay", "Dis-Chem" };
    private static readonly string[] ForeignCountries = { "NG", "RU", "CN", "BR", "US" };

    public TransactionSimulator(IServiceScopeFactory scopes, IConfiguration config, ILogger<TransactionSimulator> log)
    {
        _scopes = scopes; _config = config; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_config.GetValue("Simulator:Enabled", true)) return;
        await Task.Delay(2000, ct); // let the DB seed finish
        _log.LogInformation("Transaction simulator started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var acc = Accounts[_rng.Next(Accounts.Length)];
                var roll = _rng.Next(100);

                if (roll < 4)            // velocity burst of micro-transactions
                {
                    for (int i = 0; i < 7; i++)
                    {
                        await Send(acc, _rng.Next(5, 30), "ZA");
                        await Task.Delay(100, ct);
                    }
                }
                else if (roll < 9)       // large amount
                    await Send(acc, _rng.Next(20_000, 90_000), "ZA");
                else if (roll < 16)      // foreign location
                    await Send(acc, _rng.Next(100, 3000), ForeignCountries[_rng.Next(ForeignCountries.Length)]);
                else                     // normal
                    await Send(acc, _rng.Next(20, 2500), "ZA");

                await Task.Delay(_rng.Next(600, 2000), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _log.LogError(ex, "Simulator error"); await Task.Delay(2000, ct); }
        }
    }

    private async Task Send(string account, decimal amount, string country)
    {
        using var scope = _scopes.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<TransactionService>();
        await svc.ProcessAsync(new TransactionRequest(account, amount, Merchants[_rng.Next(Merchants.Length)], country));
    }
}
