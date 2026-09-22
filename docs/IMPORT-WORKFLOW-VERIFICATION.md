# Question import workflow verification

Use this checklist after applying both patches and running the migrations.

1. Sign in as an administrator and open **Question Bank Import**.
2. Select a certification and upload `tests/fixtures/question-bank-valid.csv`.
3. Confirm the import reaches **ReadyForReview** and reports two extracted questions.
4. Open the review screen. Edit the second question's explanation and save it.
5. Approve the first question, reject the second, and confirm both statuses persist after refreshing.
6. Publish the question-bank version. Confirm the older published version is archived.
7. Sign in as a learner. Confirm the approved question appears in Practice and can be included in a Mock Exam.
8. Repeat the upload with one PDF and one XLSX source. Confirm validation errors are shown as review issues rather than publishing invalid questions.

The API sequence is: `POST /api/imports`, `GET /api/imports/{jobId}`, `PUT /api/imports/questions/{id}`, approval/rejection endpoints, then the version publish endpoint. Never publish a version that still has pending-review questions.

## Required regression checks

- Unsupported file types and empty uploads return HTTP 400.
- Duplicate questions are marked and cannot silently create duplicate live questions.
- Rejected questions never appear in learner endpoints.
- Practice and Mock Exam only read `Published` questions from the published version.
- A learner cannot call any import/review/publish endpoint.
