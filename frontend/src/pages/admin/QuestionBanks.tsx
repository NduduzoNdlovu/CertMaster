import { useState } from "react";
import { Link } from "react-router-dom";
import { UploadCloud, FileText, AlertTriangle, ExternalLink } from "lucide-react";
import { useCertifications } from "../../hooks/useApiData";
import { useCertificationVersions, useImportJobs, useStartImport } from "../../hooks/useImportData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";
import type { ImportJobStatusValue, QuestionBankVersionStatus } from "../../types";

function versionBadgeTone(status: QuestionBankVersionStatus) {
  if (status === "Published") return "success" as const;
  if (status === "PendingReview" || status === "ImportInProgress") return "warning" as const;
  if (status === "Archived") return "neutral" as const;
  return "info" as const;
}

function jobBadgeTone(status: ImportJobStatusValue) {
  if (status === "ReadyForReview") return "warning" as const;
  if (status === "Failed") return "error" as const;
  return "info" as const;
}

export default function QuestionBanks() {
  const { data: certifications } = useCertifications();
  const [certificationId, setCertificationId] = useState<string>("");
  const [versionChoice, setVersionChoice] = useState<string>("__new__");
  const [newVersionLabel, setNewVersionLabel] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { data: versions } = useCertificationVersions(certificationId || undefined);
  const { data: jobs } = useImportJobs(certificationId || undefined);
  const startImport = useStartImport();

  const openVersions = versions?.filter((v) => v.status !== "Published" && v.status !== "Archived") ?? [];

  const handleUpload = () => {
    if (!file || !certificationId) return;
    setError(null);

    startImport.mutate(
      {
        certificationId,
        questionBankVersionId: versionChoice === "__new__" ? undefined : versionChoice,
        newVersionLabel: versionChoice === "__new__" ? newVersionLabel || undefined : undefined,
        file,
      },
      {
        onSuccess: () => {
          setFile(null);
          setNewVersionLabel("");
        },
        onError: (err: any) => setError(err?.response?.data?.error ?? "Import failed. Check the file and try again."),
      }
    );
  };

  return (
    <div className="space-y-6">
      <SectionHeading
        title="Question banks"
        description="Import → review → publish. Nothing an administrator uploads reaches learners until it's reviewed and the version is explicitly published."
      />

      <Card className="p-6 space-y-5">
        <h3 className="font-semibold text-text-primary">Start a new import</h3>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-text-primary mb-1.5">1. Certification</label>
            <select
              value={certificationId}
              onChange={(e) => { setCertificationId(e.target.value); setVersionChoice("__new__"); }}
              className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm"
            >
              <option value="">Select a certification...</option>
              {certifications?.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-text-primary mb-1.5">2. Question-bank version</label>
            <select
              value={versionChoice}
              onChange={(e) => setVersionChoice(e.target.value)}
              disabled={!certificationId}
              className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm disabled:bg-bg-alt"
            >
              <option value="__new__">Create a new version...</option>
              {openVersions.map((v) => (
                <option key={v.id} value={v.id}>{v.versionLabel} ({v.status})</option>
              ))}
            </select>
          </div>
        </div>

        {versionChoice === "__new__" && certificationId && (
          <div>
            <label className="block text-sm font-medium text-text-primary mb-1.5">New version label (optional)</label>
            <input
              value={newVersionLabel}
              onChange={(e) => setNewVersionLabel(e.target.value)}
              placeholder="e.g. v2 - Q3 2026 update"
              className="w-full max-w-sm h-11 px-3 rounded-md border border-border-subtle bg-white text-sm"
            />
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-text-primary mb-1.5">3. Upload file</label>
          <label className="flex flex-col items-center justify-center gap-2 border-2 border-dashed border-border-subtle rounded-md p-8 cursor-pointer hover:bg-bg-alt">
            <UploadCloud size={28} className="text-brand-primary" />
            <span className="text-sm font-medium text-text-primary">Click to select a file or drag it here</span>
            <span className="text-xs text-text-secondary">PDF, DOCX, XLSX, CSV, or TXT — up to 50MB</span>
            <input
              type="file"
              className="hidden"
              accept=".pdf,.docx,.xlsx,.csv,.txt"
              onChange={(e) => { setFile(e.target.files?.[0] ?? null); setError(null); }}
            />
          </label>
        </div>

        {file && (
          <div className="flex items-center justify-between p-3 bg-bg-alt rounded-md">
            <span className="flex items-center gap-2 text-sm text-text-primary"><FileText size={16} /> {file.name}</span>
            <Button size="sm" onClick={handleUpload} disabled={!certificationId || startImport.isPending}>
              {startImport.isPending ? "Processing..." : "Start import"}
            </Button>
          </div>
        )}

        {error && (
          <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-state-error">
            <AlertTriangle size={16} className="mt-0.5 shrink-0" /> {error}
          </div>
        )}

        {startImport.data && (
          <div className={`p-4 rounded-md border ${startImport.data.status === "Failed" ? "bg-red-50 border-red-200" : "bg-green-50 border-green-200"}`}>
            {startImport.data.status === "Failed" ? (
              <p className="text-sm text-state-error">Import failed: {startImport.data.errorMessage}</p>
            ) : (
              <>
                <p className="text-sm font-semibold text-state-success mb-2">
                  Import complete — {startImport.data.questionsExtracted} question(s) extracted, all awaiting review.
                </p>
                <Link
                  to={`/admin/imports/${startImport.data.id}`}
                  className="inline-flex items-center gap-1 text-sm font-semibold text-brand-primary hover:underline"
                >
                  Review this import <ExternalLink size={14} />
                </Link>
              </>
            )}
          </div>
        )}
      </Card>

      {certificationId && (
        <>
          <Card className="p-0 overflow-x-auto">
            <div className="px-5 py-3 border-b border-border-subtle">
              <h3 className="font-semibold text-text-primary text-sm">Question-bank versions</h3>
            </div>
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-text-secondary bg-bg-alt">
                  <th className="py-3 px-5 font-medium">Version</th>
                  <th className="py-3 px-5 font-medium">Status</th>
                  <th className="py-3 px-5 font-medium">Extracted</th>
                  <th className="py-3 px-5 font-medium">Flagged</th>
                  <th className="py-3 px-5 font-medium">Duplicates</th>
                  <th className="py-3 px-5 font-medium">Published</th>
                </tr>
              </thead>
              <tbody>
                {versions?.map((v) => (
                  <tr key={v.id} className="border-t border-border-subtle">
                    <td className="py-3 px-5 text-text-primary font-medium">{v.versionLabel}</td>
                    <td className="py-3 px-5"><Badge tone={versionBadgeTone(v.status)}>{v.status}</Badge></td>
                    <td className="py-3 px-5 text-text-secondary">{v.questionsExtracted}</td>
                    <td className="py-3 px-5 text-text-secondary">{v.questionsFlagged}</td>
                    <td className="py-3 px-5 text-text-secondary">{v.duplicatesDetected}</td>
                    <td className="py-3 px-5 text-text-secondary">
                      {v.publishedAtUtc ? new Date(v.publishedAtUtc).toLocaleDateString() : "—"}
                    </td>
                  </tr>
                ))}
                {versions?.length === 0 && (
                  <tr><td colSpan={6} className="py-6 px-5 text-center text-text-secondary">No versions yet for this certification.</td></tr>
                )}
              </tbody>
            </table>
          </Card>

          <Card className="p-0 overflow-x-auto">
            <div className="px-5 py-3 border-b border-border-subtle">
              <h3 className="font-semibold text-text-primary text-sm">Import jobs</h3>
            </div>
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-text-secondary bg-bg-alt">
                  <th className="py-3 px-5 font-medium">File</th>
                  <th className="py-3 px-5 font-medium">Status</th>
                  <th className="py-3 px-5 font-medium">Pending / Approved / Rejected</th>
                  <th className="py-3 px-5 font-medium">Uploaded</th>
                  <th className="py-3 px-5 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {jobs?.map((j) => (
                  <tr key={j.id} className="border-t border-border-subtle">
                    <td className="py-3 px-5 text-text-primary font-medium">{j.originalFileName}</td>
                    <td className="py-3 px-5"><Badge tone={jobBadgeTone(j.status)}>{j.status}</Badge></td>
                    <td className="py-3 px-5 text-text-secondary">
                      {j.questionsPendingReview} / {j.questionsApproved} / {j.questionsRejected}
                    </td>
                    <td className="py-3 px-5 text-text-secondary">{new Date(j.createdAtUtc).toLocaleString()}</td>
                    <td className="py-3 px-5">
                      <Link to={`/admin/imports/${j.id}`} className="text-brand-primary text-sm font-medium hover:underline">
                        Review
                      </Link>
                    </td>
                  </tr>
                ))}
                {jobs?.length === 0 && (
                  <tr><td colSpan={5} className="py-6 px-5 text-center text-text-secondary">No import jobs yet for this certification.</td></tr>
                )}
              </tbody>
            </table>
          </Card>
        </>
      )}
    </div>
  );
}
