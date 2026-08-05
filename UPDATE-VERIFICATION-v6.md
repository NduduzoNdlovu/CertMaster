# CertMaster v6 — Question-Bank Import Workflow

## What changed

This release replaces the old "upload → auto-publish" dump upload engine with a full
traceable import → review → publish pipeline, matching this workflow exactly:

```
Select certification → Create/select version → Upload file → Validate file →
Store file securely → Create import job → Extract text and images →
Detect question boundaries → Extract options → Extract correct answers →
Extract explanations → Normalise content → Detect duplicates/validation problems →
Administrator reviews → Administrator corrects or rejects → Administrator approves →
Publish version → Used in Practice and Mock Exams
```

**The core rule this release enforces: nothing reaches Practice or Mock Exams without
an administrator explicitly approving it and publishing its version.** This includes
local development seed data — even the sample questions now go through this exact
pipeline (simulating an admin's review) rather than being inserted directly.

## Judgment calls made explicit

- **File formats kept, not narrowed to PDF-only.** The workflow diagram names PDF, but
  the real requirement is the review gate, not a format restriction. PDF, DOCX, XLSX,
  CSV, and TXT all flow through the same pipeline.
- **Processing is synchronous.** No background job infrastructure exists yet, so
  extraction happens within the upload request/response cycle. Large PDFs may take a
  few seconds. The status field (`Uploaded → Validating → Extracting → ... →
  ReadyForReview`/`Failed`) is modeled so a future background worker could pick this up
  without changing the data model.
- **PDF image extraction is best-effort and job-level, not question-level.** Images are
  extracted and stored for the admin to view during review, but automatically matching
  an image to the specific question it illustrates needs real page-layout analysis,
  which this release doesn't attempt.
- **"Authorised PDF" validation** = administrator-only endpoint (already required) +
  file-extension check + a file-signature (magic-byte) check that the file actually
  starts with `%PDF-`. This is not virus scanning — that's still explicitly deferred.

## New backend structure

- **New entities**: `ImportJob` (one per uploaded file, tracks status/errors/counts —
  the traceability record), `ImportJobImage` (extracted PDF images), `ImportedQuestion`
  + `ImportedQuestionOption` (staged content awaiting review — a completely separate
  table from the live `Question` table).
- **`QuestionBankVersion` reworked**: the old `IsPublished` bool is replaced with a real
  `Status` lifecycle (`Draft → ImportInProgress → PendingReview → Published/Archived`).
  `UploadedByUserId`/`SourceFileName`/`SourceFormat` moved to `ImportJob` (a version can
  now receive multiple import jobs before being published as a whole).
- **`IFileStorage`** — uploaded files are now saved to disk outside any web-servable
  path (`App_Data/uploads` by default, GUID-named, SHA256-hashed), never directly
  reachable by URL. Swap the registered implementation to move to S3/Azure Blob later.
- **`ImportService`** — the full pipeline, reusing your existing 5 file parsers for the
  extraction step. New `TextNormalizer` handles the normalization step.
- **`ImportController`** — replaces the old `QuestionBankUploadController` entirely.
- **`DbSeeder`** rewritten to build an in-memory CSV from the sample question data and
  push it through `ImportService` (import → approve → publish) instead of inserting
  `Question` rows directly.

## ⚠️ Database migration — this one is bigger than usual

This release changes the shape of `QuestionBankVersion` (columns removed/renamed) and
adds four new tables. You have two options:

### Option A (recommended): start your dev database fresh

Since this entire release is about replacing demo/seeded content with a real workflow,
and your existing local data is disposable dev/test data, the cleanest path is:

```powershell
# In psql, or your preferred client:
DROP DATABASE certmaster_dev;
CREATE DATABASE certmaster_dev;
```

Then delete your existing migration files and regenerate from scratch:

```powershell
cd backend
# Remove the old Migrations folder under CertMaster.Infrastructure, then:
dotnet ef migrations add InitialCreate --project src/CertMaster.Infrastructure --startup-project src/CertMaster.Api
dotnet run --project src/CertMaster.Api
```

The seeder will recreate your admin account and push the sample questions through the
real import pipeline automatically on first run.

### Option B: keep your existing database, add an incremental migration

```powershell
cd backend
dotnet ef migrations add QuestionBankImportWorkflow --project src/CertMaster.Infrastructure --startup-project src/CertMaster.Api
dotnet run --project src/CertMaster.Api
```

This works, but any `QuestionBankVersion` rows that existed before this migration will
show `Status = Draft` afterward (the new column has no historical data to infer from).
This is **cosmetic only** — it doesn't affect which questions learners see, since
Practice/Mock Exam always filter by the individual `Question.Status`, not by version
status. It only means an old version's status badge in the admin UI will look
inaccurate until you republish or ignore it.

## API changes

- **Removed**: `POST /api/admin/question-banks/upload` (the old auto-publish endpoint)
- **New**:
  - `GET /api/admin/certifications/{id}/versions`
  - `POST /api/admin/imports` (multipart: certificationId, questionBankVersionId?, newVersionLabel?, file)
  - `GET /api/admin/imports?certificationId=`
  - `GET /api/admin/imports/{id}`
  - `GET /api/admin/imports/images/{imageId}`
  - `PUT /api/admin/imports/questions/{id}`
  - `POST /api/admin/imports/questions/approve` — `{ importedQuestionIds: [] }`
  - `POST /api/admin/imports/questions/reject` — `{ importedQuestionIds: [], reason }`
  - `POST /api/admin/versions/{versionId}/publish`

## Frontend changes

- **Admin → Question Banks** rebuilt around the workflow: select certification → select
  an existing draft/in-review version or create a new one → upload → see the resulting
  versions table and import-jobs table for that certification.
- **New Admin → Import Review page** (`/admin/imports/:jobId`): inline-editable question
  cards showing validation issues and duplicate flags, per-question approve/reject, a
  "approve all clean questions" bulk action, and a publish button that's disabled until
  every question in the job has been reviewed.
- **Practice and Mock Exam** now show a proper empty state ("no published questions
  yet") instead of crashing — a real scenario now that certifications legitimately
  start with zero questions until an admin publishes something.

## Configuration

New `Storage` section in `appsettings.json` (`UploadRoot`, defaults to
`App_Data/uploads`) — added as a new section, your existing config values (connection
string, JWT key, email, everything else) are untouched. Add `App_Data/` to your local
`.gitignore` if you haven't already pulled that change (already done in this patch).

## Verify it worked

1. Log in as admin, go to Question Banks, select a certification.
2. Upload a small CSV using this header format (see `CsvDumpParser.cs` for the exact
   column names): `Topic,Subtopic,Difficulty,Prompt,Option1,Option2,Option3,Option4,CorrectOptionIndex,Explanation,Reference`
3. You should land on (or be able to navigate to) the Import Review page and see your
   questions staged as `PendingReview`, with any validation issues flagged.
4. Try approving one, rejecting another, editing a third before approving it.
5. Confirm the Publish button stays disabled until every question is reviewed.
6. Publish, then check Practice mode for that certification shows the new question.
7. Confirm a fresh database (or one you haven't touched) shows **zero** questions for a
   certification until you complete this flow — nothing pre-populated bypasses review.

## What I still couldn't verify

No .NET SDK/NuGet access in this environment. The riskiest single piece I couldn't
verify by compiling is `ImportedQuestion`'s PDF image extraction, which calls
`image.TryGetPng(out var pngBytes)` on a PdfPig `IPdfImage` — I'm reasonably but not
certainly confident this method exists with this exact signature in PdfPig 0.1.9. It's
wrapped in a try/catch, so if the API differs, text-based question extraction is
unaffected either way — only image extraction would silently no-op. If `dotnet build`
flags this line, check PdfPig's `IPdfImage` interface for the current image-bytes
accessor in your installed version.

Frontend is type-checked and build-verified successfully.
