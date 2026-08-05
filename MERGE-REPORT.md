# CertMaster merge report

This folder uses the working `CertMaster(1).zip` configuration as its baseline and
adds the source/features supplied in `CertMaster-merged.zip` through the v6
question-bank import workflow.

## Preserved working configuration

- Backend development connection string and JWT settings
- Backend launch profile
- Frontend `.env` (`VITE_USE_MOCKS=false` and the configured API URL)
- Frontend API base URL behaviour
- Existing EF Core package fixes and ASP.NET Core framework reference
- Existing `InitialCreate` and `AddNotifications` migrations

## Added functionality

- PDF, DOCX, XLSX, CSV and TXT question-bank imports
- Import staging, duplicate/validation detection and administrator review
- Version publishing gate before questions become available to learners
- Bookmarks and question reports
- Profile and password updates
- Real leaderboard and study streak tracking
- Notifications, search, flashcards, billing abstraction and Redis-ready caching
- Updated learner Practice and Mock Exam flows
- Updated admin Question Banks, Import Review and Reports pages

## Validation completed

- Frontend dependencies installed successfully with `npm ci`.
- TypeScript compilation and the Vite production build completed successfully.
- The PDF library contained the `TryGetPng` API used by the import workflow.
- Working configuration values were compared and preserved.

The current execution environment did not contain the .NET SDK, so the backend
could not be compiled here. Before the first backend run, generate the v6 database
migration with the installed .NET SDK:

```powershell
cd backend
dotnet restore
dotnet ef migrations add QuestionBankImportWorkflow --project src/CertMaster.Infrastructure --startup-project src/CertMaster.Api
dotnet build
dotnet run --project src/CertMaster.Api
```

The API applies pending migrations automatically when running in Development.
Using the commands above preserves the existing configured database. If its current
data is disposable, the clean-database option described in
`UPDATE-VERIFICATION-v6.md` is recommended.

Run the frontend in a second terminal:

```powershell
cd frontend
npm install
npm run dev
```
