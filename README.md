# Real-Time Transaction Processing & Fraud Monitoring (Simulation)

ASP.NET Core (.NET 8) + EF Core + SQLite. No external database or services required.

## Run in Visual Studio
1. Open the folder in Visual Studio 2022 (File > Open > Project/Solution > FraudMonitor.csproj).
2. Let NuGet restore packages.
3. Press F5. The app creates `fraudmonitor.db` automatically and seeds 5 accounts.
4. Browser opens the API; go to `/` for the live dashboard or `/swagger` for the API.

Transactions are created manually from the "New transaction" form on the dashboard
(or via POST /api/transactions in Swagger). The automatic traffic simulator is OFF by default;
set `"Simulator": { "Enabled": true }` in appsettings.json to turn it back on.

## Fraud rules (Services/FraudRuleEngine.cs)
| Rule | Trigger | Score |
|---|---|---|
| LARGE_AMOUNT | >= R20,000 / >= R50,000 | 45 / 80 |
| HIGH_VELOCITY | 5+ transactions in 60s on one account | 75 |
| FOREIGN_LOCATION | Country differs from account home country | 50 |
| IMPOSSIBLE_TRAVEL | Country change within 10 minutes | 80 |

Scores add up (max 100): >= 70 Blocked, >= 40 Flagged, otherwise Settled.
Funds are only debited when a transaction is Settled, so fraud is stopped before settlement.

## Manual test (Swagger or curl)
POST /api/transactions
{ "accountNumber": "1001001", "amount": 75000, "merchant": "Test", "country": "ZA" }
