# CertMaster — Easy Wins Round

Skipped for now per your request: real-time chat (SignalR) and upload virus scanning.

## What was added

- **Bookmarks** — real backend (`Bookmark` entity was already there, unused). New
  `/api/bookmarks` endpoints; the bookmark icon in Practice mode now actually saves/removes,
  and the Bookmarks page shows your real saved questions with a link back to practice that topic.
- **Question reports** — the flag icon in Practice mode now opens a reason box and submits
  to `/api/question-reports`; admins can review open reports via
  `GET /api/question-reports/open` and resolve them (`POST /api/question-reports/{id}/resolve`).
  No admin UI page for this yet — reachable via Swagger/API for now.
- **Settings page** — "Save changes" now updates your name via `PUT /api/profile`;
  "Update password" now verifies your current password and updates it via
  `POST /api/profile/change-password`. Email field is read-only (changing it would need
  re-verification, which is out of scope for this round).
- **Real Leaderboard** — replaced static demo rows with `GET /api/leaderboard` and
  `GET /api/leaderboard/me`, computed as 10 points per correct answer + 50 per passed mock
  exam, grouped by user.
- **Study streak** — `User.StudyStreakDays` now actually updates whenever an exam attempt
  (practice or mock) is submitted: unchanged if already active today, +1 if the last activity
  was yesterday, reset to 1 otherwise.

## Configuration

Nothing in `appsettings.Development.json`, `launchSettings.json`, `.env`, or any of your
previous customizations was touched.

## Database

All of this uses existing tables (`Bookmarks`, `QuestionReports`, `Users` fields already in
your schema) — **no new migration needed** for this round, on top of the `AddNotifications`
migration from the v3 round.

## Verify locally

```powershell
cd backend
dotnet build
cd src/CertMaster.Api
dotnet run
```

```powershell
cd frontend
npm install
npm run build
npm run dev
```

Try: bookmark a question in Practice, check it shows up on the Bookmarks page; report a
question; update your name and password in Settings; complete a mock exam and check the
Leaderboard and your dashboard's study streak.
