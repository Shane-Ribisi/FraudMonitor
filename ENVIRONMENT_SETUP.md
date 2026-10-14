# FraudMonitor — Environment Setup Guide

## 1. Project Overview

FraudMonitor is a real-time transaction processing and fraud monitoring simulation built using ASP.NET Core on .NET 8, Entity Framework Core, and SQLite.

The application uses a local SQLite database and does not require an external database server or external services, according to the project README.

## 2. Prerequisites

Install or have access to the following tools:

- Visual Studio Code or Visual Studio 2022.
- The .NET 8 SDK.
- The C# extension and C# Dev Kit when using Visual Studio Code.
- Git, if cloning the source code from GitHub.

Verify the .NET installation by running:

```powershell
dotnet --list-sdks
dotnet --list-runtimes
```

## 3. Obtain and Open the Project

If the project has not already been downloaded, clone the repository:

```powershell
git clone https://github.com/Shane-Ribisi/FraudMonitor.git
cd FraudMonitor/FraudMonitor
```

If the repository is already available locally, open the folder containing `FraudMonitor.csproj` in Visual Studio Code.

## 4. Restore Dependencies

Open a terminal in the directory containing `FraudMonitor.csproj` and run:

```powershell
dotnet restore
```

This restores the NuGet packages required by the project.

The main dependencies are:

- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.EntityFrameworkCore.Design
- Serilog.AspNetCore
- Swashbuckle.AspNetCore

The package versions are defined in `FraudMonitor.csproj`.

## 5. Build the Application

Run the following command:

```powershell
dotnet build
```

Confirm that the build completes successfully before proceeding.

## 6. Run the Application

Start the application with:

```powershell
dotnet run
```

Follow the terminal output to identify the local URL and port assigned to the application.

Open the application in a browser:

- `/` — live dashboard.
- `/swagger` — API documentation and testing interface.

Use the base URL shown in the terminal, followed by the relevant path.

## 7. Database Initialization

The project README states that the application creates `fraudmonitor.db` automatically and seeds five accounts when the application starts.

No separate database server is required for the documented setup.

## 8. Simulator Configuration

The automatic transaction traffic simulator is disabled by default.

To enable it, configure the following setting in `appsettings.json`:

```json
"Simulator": {
  "Enabled": true
}
```

Keep the simulator disabled when testing manually unless automatic traffic is required.

## 9. Test a Transaction

Use the Swagger interface to send a `POST` request to `/api/transactions` with a test request body:

```json
{
  "accountNumber": "1001001",
  "amount": 75000,
  "merchant": "Test",
  "country": "ZA"
}
```

Review the API response and dashboard to verify the application's behaviour.

## 10. Troubleshooting

- If the .NET SDK cannot be found, verify the installation and restart the terminal.
- If package restoration fails, check network access to the configured NuGet source.
- If the application does not build, review the build errors in the terminal.
- If the browser URL does not work, use the address and port printed by `dotnet run`.
- If the database or application fails to initialize, review the terminal output and application logs.

## 11. Setup Verification Checklist

- [ ] .NET SDK is installed.
- [ ] Project dependencies restore successfully.
- [ ] Application builds successfully.
- [ ] Application starts and displays its local URL.
- [ ] Dashboard or Swagger page opens.
- [ ] Database initialization is verified.
- [ ] A test transaction is submitted and its response is reviewed.

**Document purpose:** To help developers set up, run, and verify the FraudMonitor development environment consistently.