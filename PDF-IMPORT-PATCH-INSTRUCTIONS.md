# CertMaster PDF import patch

This patch upgrades the existing import workflow without replacing existing data.

## Included

- A+ headings such as `Question #:109`
- Network+ headings such as `Question: 7`
- `A.`, `A)`, `A:` and bulleted option formats
- answers on the same or following line
- multiple correct answers such as `Answer: D E`
- PDF source-page tracking
- PDF image extraction, duplicate decoration filtering and question association
- PBQ/simulation detection and a mandatory manual-review gate
- authenticated image previews in the admin review screen
- EF Core schema migration

Static PDFs do not contain the original behaviour of interactive PBQs. The patch
preserves their text and images and blocks approval until an administrator reviews
or converts them. It does not pretend to recreate unavailable CompTIA scoring rules.

## Apply

From the CertMaster repository root:

```powershell
git apply --check .\certmaster-pdf-image-pbq-import.patch
git apply .\certmaster-pdf-image-pbq-import.patch
```

Then apply the included migration and build:

```powershell
cd backend
dotnet restore .\CertMaster.sln
dotnet build .\CertMaster.sln

dotnet ef database update `
  --project .\src\CertMaster.Infrastructure\CertMaster.Infrastructure.csproj `
  --startup-project .\src\CertMaster.Api\CertMaster.Api.csproj

cd ..\frontend
npm install
npm run build
```

Delete the old zero-question import versions or leave them unpublished. Upload the
PDF again into a new question-bank version, then inspect flagged PBQs before publish.
