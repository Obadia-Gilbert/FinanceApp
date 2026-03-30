# Shared configuration

**`appsettings.shared.json`** is loaded by **FinanceApp.Web** and **FinanceApp.API** (see `Program.cs` in each project). It centralizes:

- **`ConnectionStrings:DefaultConnection`** — one database for MVC + API + mobile (override with User Secrets or environment variables per machine).
- **`SubscriptionBilling`** — Apple / Google product id → plan mapping and Play service account path.

Copy or symlink is not required: both hosts resolve `../Shared/appsettings.shared.json` relative to each project’s content root.

For production, prefer environment variables or a secrets store for connection strings and `ServiceAccountJsonPath`; keep product id keys aligned with App Store Connect and Google Play Console.
