# CertMaster exam/practice start and mock-submit fix

This patch addresses two issues found in the supplied project snapshot:

1. The database enforces **one unfinished Mock Exam per user**, but `ExamService` searched for an unfinished Mock Exam only under the selected certification. If an unfinished Mock Exam existed under another certification, the service tried to insert another attempt and PostgreSQL rejected it with `IX_ExamAttempts_UserId_OneActiveMock`.
2. Vite's `/api` proxy used `https://localhost:5000`, while the supplied ASP.NET launch profile listens on `http://localhost:5000`. That protocol mismatch can produce `EPROTO ... packet length too long` during `/api/auth/refresh`. If refresh is triggered while submitting, the frontend can show a generic submission failure without a useful backend error.

## Apply the patch

1. Stop the Vite frontend and ASP.NET backend.
2. Back up your current changes or commit them to Git first.
3. Copy `CERTMASTER-EXAM-START-SUBMIT-FIX.patch` into the CertMaster repository root, alongside the `frontend` and `backend` folders.
4. In Command Prompt, run:

```bat
git apply --check --ignore-whitespace --whitespace=nowarn ".\CERTMASTER-EXAM-START-SUBMIT-FIX.patch"
```

5. If the check succeeds, apply it:

```bat
git apply --ignore-whitespace --whitespace=nowarn ".\CERTMASTER-EXAM-START-SUBMIT-FIX.patch"
git diff --check
git diff --stat
```

If the check fails, **do not force it**. Share the exact error and we can adjust it to your current files.

## Build and restart

From the backend folder:

```bat
dotnet build
```

From the frontend folder:

```bat
npm run build
```

Restart the backend and Vite. Since `vite.config.ts` changed, fully stop and restart the Vite dev server; hot reload alone does not reload the proxy configuration.

## Verify

- Open Practice and start a session.
- Start a Mock Exam. If you already have an unfinished Mock Exam, the service should resume it instead of creating a second active Mock Exam for the same user.
- Submit a Mock Exam and check the browser Network tab for `POST /api/exams/{attemptId}/complete` and its HTTP status.
- Confirm `/api/auth/refresh` no longer logs the HTTPS-to-HTTP `EPROTO` proxy error.

This patch has been checked for valid diff formatting against the supplied project snapshot. Run the builds above against your current working tree before relying on it; the patch has not been compiled against your local, possibly newer changes.
