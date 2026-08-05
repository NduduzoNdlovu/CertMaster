# CertMaster v3 Merge Verification

This merge was done file-by-file against your actual working project (not a blind
overwrite), specifically to avoid losing your fixes and local configuration.

## Your customizations that were detected and preserved untouched

- `backend/src/CertMaster.Api/appsettings.Development.json` — your real DB connection
  string and JWT signing key
- `backend/src/CertMaster.Api/Properties/launchSettings.json`
- `frontend/.env` — your `VITE_USE_MOCKS=false` and added `VITE_API_URL` setting
- `frontend/src/lib/api.ts` — your `baseURL` change to use `VITE_API_URL` directly
  instead of the Vite dev proxy
- `frontend/src/components/charts/ActivityChart.tsx` and `RegistrationsChart.tsx` —
  your `data = []` default/optional-prop fix
- `frontend/src/pages/learner/Practice.tsx` — your `question?.` optional-chaining fix
- `backend/src/CertMaster.Infrastructure/Persistence/Seed/SeedQuestionBank.cs` and
  `frontend/src/lib/mockData.ts` — your added 32nd Network+ question (APIPA/DHCP) is
  still there; `mockData.ts` only got the new v3 additions appended around it
- `backend/src/CertMaster.Infrastructure/CertMaster.Infrastructure.csproj` — your
  EF Core 9.0.1 version bump, `Microsoft.Extensions.Options.ConfigurationExtensions`
  package, and `FrameworkReference` to `Microsoft.AspNetCore.App` were all kept exactly;
  the 5 new v3 packages were added alongside them, not in place of them
- Your applied EF migration (`20260730093640_InitialCreate.cs` and related files) —
  untouched
- `frontend/src/assets/hero.png` and any other custom assets — untouched

## What was added (the v3 feature round)

Dump upload engine (CSV/XLSX/DOCX/TXT/PDF parsing), search, notifications, flashcards,
exam readiness score, email sending (console-logged by default), payments (mock
gateway), and Redis-ready caching for the certifications list. See `UPGRADE-v3.md` for
the full feature list.

## One thing you still need to do: a new migration

Your database already has every table these features need **except** `Notifications`
(new in this round — `PaymentTransactions` was already in your schema). Run:

```bash
cd backend
dotnet ef migrations add AddNotifications --project src/CertMaster.Infrastructure --startup-project src/CertMaster.Api
```

The API applies pending migrations automatically on startup in Development, so you
don't need to run `dotnet ef database update` separately — just start the API as usual
after adding the migration.

## Local verification commands

```powershell
cd backend
dotnet restore
dotnet build
cd src/CertMaster.Api
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```

```powershell
cd frontend
npm install
npm run build
npm run dev
```

Since 5 new NuGet packages were added, `dotnet restore` will need network access to
NuGet the first time you build after this merge.

## What I could not verify

I don't have a .NET SDK or NuGet access in the environment I did this merge in, so the
backend C# was written and merged carefully but **not compiled**. The frontend was
type-checked and built successfully. Run `dotnet build` locally as your first step and
let me know if anything doesn't compile — most likely candidates would be a missed
`using` statement, which is a quick fix.
