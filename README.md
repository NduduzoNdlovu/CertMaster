# CertMaster

**Learn. Practice. Pass.**

An IT certification exam preparation platform (initial focus: CompTIA A+ and Network+). This repo contains:

- `frontend/` — React + TypeScript + Vite + Tailwind CSS learner and admin web app
- `backend/` — ASP.NET Core 9 Web API (Clean Architecture: Domain / Application / Infrastructure / Api), EF Core, PostgreSQL, JWT auth

> **Status:** this is a working foundation, not the complete platform described in the original brief (that's a multi-month build for a team). The frontend runs today. The backend is source-complete for its current scope (auth, certifications, practice, mock exams, dashboard, admin) but was written without a local .NET SDK available, so **build it locally as your first step** and fix any small issues `dotnet build` surfaces before relying on it.

---

## 1. Prerequisites

Install these before you start:

| Tool | Version | Check with |
|---|---|---|
| Node.js | 20 LTS or newer | `node -v` |
| npm | 10+ (comes with Node) | `npm -v` |
| .NET SDK | 9.0 | `dotnet --version` |
| PostgreSQL | 16+ | `psql --version` |
| Git | any recent version | `git --version` |

Download links:
- Node.js: https://nodejs.org
- .NET 9 SDK: https://dotnet.microsoft.com/download/dotnet/9.0
- PostgreSQL: https://www.postgresql.org/download/

---

## 2. Quick start — frontend only (no backend needed)

The frontend ships with **mock mode on by default**, so you can run and click through the entire UI (dashboard, practice, mock exams, admin panel) without setting up the database or API at all.

```bash
cd frontend
npm install
npm run dev
```

Open the URL Vite prints (usually **http://localhost:5173**). Log in with any email/password — mock mode accepts anything.

To turn mock mode off once your backend is running, edit `frontend/.env`:

```
VITE_USE_MOCKS=false
```

---

## 3. Full setup — frontend + backend + database

### 3.1 Create the PostgreSQL database

```bash
psql -U postgres
```

Then, inside `psql`:

```sql
CREATE DATABASE certmaster_dev;
CREATE USER certmaster_user WITH PASSWORD 'ChangeMe123!';
GRANT ALL PRIVILEGES ON DATABASE certmaster_dev TO certmaster_user;
\q
```

### 3.2 Configure the backend

Open `backend/src/CertMaster.Api/appsettings.Development.json` and update:

- `ConnectionStrings:DefaultConnection` — match the database/user/password you created above
- `Jwt:SigningKey` — replace with a random string at least 32 characters long (e.g. generate one with `openssl rand -base64 48`)

For anything beyond local development, use .NET user-secrets or environment variables instead of committing real secrets to `appsettings.json`:

```bash
cd backend/src/CertMaster.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "your-long-random-secret"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=certmaster_dev;Username=certmaster_user;Password=ChangeMe123!"
```

### 3.3 Install EF Core tools (one-time, if you don't already have them)

```bash
dotnet tool install --global dotnet-ef
```

### 3.4 Create and apply the initial migration

```bash
cd backend
dotnet restore
dotnet ef migrations add InitialCreate --project src/CertMaster.Infrastructure --startup-project src/CertMaster.Api
```

You don't need to run `dotnet ef database update` manually — the API applies pending migrations automatically on startup **in Development**. It also seeds:
- Three certifications (A+ Core 1, A+ Core 2, Network+) with one sample question
- An administrator account: **admin@certmaster.local** / **ChangeMe123!** (change this password immediately in any shared environment)

### 3.5 Run the backend

```bash
cd backend/src/CertMaster.Api
dotnet run
```

The API starts on the ports shown in the console (typically `https://localhost:5001` and `http://localhost:5000`). Swagger UI is available at `https://localhost:5001/swagger` in Development.

If your ports differ from `5001`, update the proxy target in `frontend/vite.config.ts` (`server.proxy['/api'].target`) to match.

### 3.6 Run the frontend against the real backend

```bash
cd frontend
# edit .env: VITE_USE_MOCKS=false
npm install
npm run dev
```

Visit **http://localhost:5173**, register a new learner account (or log in as the seeded admin), and the app will now be talking to your real API and database.

---

## 4. Project structure

```
CertMaster/
├── frontend/                     React + TypeScript + Vite + Tailwind
│   ├── src/
│   │   ├── components/           layout (header/sidebar/mobile nav), ui primitives, charts
│   │   ├── context/               AuthContext (JWT session state)
│   │   ├── hooks/                 React Query hooks (dashboard, certifications, questions, admin)
│   │   ├── lib/                   axios instance, query client, mock data
│   │   ├── pages/
│   │   │   ├── auth/               Login, Register, Forgot Password
│   │   │   ├── learner/            Dashboard, Practice, Mock Exam, Results, Progress, Bookmarks,
│   │   │   │                       Certifications, Leaderboard, Premium, Settings, Help
│   │   │   └── admin/              Overview, Users, Question Banks, Analytics, Maintenance,
│   │   │                           Payments, Logs
│   │   ├── routes/                 ProtectedRoute (auth + role guard)
│   │   ├── types/                  shared TypeScript types
│   │   └── App.tsx                 route definitions
│   └── .env                        VITE_USE_MOCKS flag
│
└── backend/                      ASP.NET Core 9 Web API — Clean Architecture
    ├── src/
    │   ├── CertMaster.Domain/          entities, enums — no dependencies
    │   ├── CertMaster.Application/     interfaces, DTOs, feature services (Auth, Certifications,
    │   │                                Questions, Exams, Dashboard, Admin)
    │   ├── CertMaster.Infrastructure/   EF Core DbContext + configurations, JWT/BCrypt services,
    │   │                                database seeder
    │   └── CertMaster.Api/             Program.cs, controllers, appsettings, middleware
    └── CertMaster.sln
```

---

## 5. How the architecture supports adding new certifications

Certifications, topics, and questions are pure data (`Certification`, `Topic`, `Question` entities) — adding CompTIA Security+, CySA+, or any other certification is a matter of inserting rows (via the admin question-bank import workflow — see `UPDATE-VERIFICATION-v6.md` — or a seed script), never a code change. The frontend certification list, practice mode, and mock exam mode all read from the same generic `/api/certifications` and `/api/questions/practice` endpoints regardless of how many certifications exist.

---

## 6. What's implemented vs. what's a stub

> This section reflects the project as of the v6 update. See `UPDATE-VERIFICATION-v6.md` for full details on the most recent changes, and the earlier `UPDATE-VERIFICATION-v*.md` files for the history of how it got here.

**Implemented and working:**
- JWT auth (register, login, refresh tokens, email verification and password reset codes), role-based access (Learner / Administrator)
- Certifications listing (Redis-backed cache, in-memory fallback), practice mode, mock exam mode — both now persist real attempts to the backend
- **Question-bank import workflow**: admin uploads a PDF/DOCX/XLSX/CSV/TXT file → file is validated and stored securely → text (and best-effort images, for PDF) is extracted → questions/options/answers/explanations are detected → content is normalized → duplicates and validation problems are flagged → an admin reviews, corrects, approves, or rejects each question → only once every question in a version is reviewed can the admin publish it, which is the only point at which content becomes visible to Practice/Mock Exams. Nothing — including local dev seed data — bypasses this review gate.
- Bookmarks, question reporting (auto-flags the question pending admin review), notifications, flashcards (Premium-gated), exam readiness score, real leaderboard (all-time/monthly), study streak tracking, billing via a swappable payment gateway abstraction (mock provider by default)
- Learner dashboard and progress analytics computed from real exam attempt data
- Admin overview, user list/suspend, maintenance window scheduling, audit log, payments list, reported-questions review
- Responsive layout (desktop sidebar, mobile drawer + bottom nav) matching the specified color palette and typography

**Known simplifications (documented, not hidden):**
- No background job queue exists yet — file processing runs synchronously within the upload request. Fine for typical file sizes; a future release could move this to a background worker without changing the data model.
- PDF image extraction is best-effort and stored per import job for admin viewing, but not automatically linked to the specific question it illustrates — that needs real page-layout analysis this release doesn't attempt.
- Real payment gateway integration, virus scanning on uploads, and real-time features (SignalR) are not implemented.

This is intentionally structured so each of those can be added without touching unrelated code.

---

## 7. Troubleshooting

- **`dotnet ef` command not found** — run `dotnet tool install --global dotnet-ef`, then restart your terminal.
- **Backend can't connect to PostgreSQL** — confirm PostgreSQL is running (`pg_isready`) and the connection string's host/port/username/password match what you created in step 3.1.
- **Frontend shows no data / network errors** — confirm `VITE_USE_MOCKS=false` in `frontend/.env` and that the backend is running; check the browser console for CORS errors (the backend's `Cors:AllowedOrigins` must include your frontend's URL).
- **Port already in use** — change the port Vite or Kestrel binds to, or stop the process using it.
