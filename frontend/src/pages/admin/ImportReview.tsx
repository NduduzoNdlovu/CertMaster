import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { CheckCircle2, XCircle, AlertTriangle, Copy, Save, Rocket } from "lucide-react";
import {
  useImportJobDetail,
  useUpdateImportedQuestion,
  useApproveImportedQuestions,
  useRejectImportedQuestions,
  usePublishVersion,
} from "../../hooks/useImportData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";
import type { ImportedQuestion } from "../../types";
import { api } from "../../lib/api";

function ImportedImage({ imageId, pageNumber }: { imageId: string; pageNumber: number }) {
  const [url, setUrl] = useState<string>();
  useEffect(() => {
    let objectUrl: string | undefined;
    api.get(`/admin/imports/images/${imageId}`, { responseType: "blob" }).then(({ data }) => {
      objectUrl = URL.createObjectURL(data);
      setUrl(objectUrl);
    });
    return () => { if (objectUrl) URL.revokeObjectURL(objectUrl); };
  }, [imageId]);
  return url ? <img src={url} alt={`Imported question media from PDF page ${pageNumber}`} className="max-h-96 max-w-full rounded-md border border-border-subtle object-contain" /> : null;
}

function EditableQuestion({ question }: { question: ImportedQuestion }) {
  const updateQuestion = useUpdateImportedQuestion();
  const approve = useApproveImportedQuestions();
  const reject = useRejectImportedQuestions();

  const [draft, setDraft] = useState(question);
  const [rejecting, setRejecting] = useState(false);
  const [rejectReason, setRejectReason] = useState("");

  useEffect(() => setDraft(question), [question]);

  const isPublished = Boolean(question.publishedQuestionId);
  const isDirty = JSON.stringify(draft) !== JSON.stringify(question);

  const handleSave = () => {
    updateQuestion.mutate({ id: question.id, question: draft });
  };

  const updateOption = (index: number, text: string) => {
    setDraft((d) => ({ ...d, options: d.options.map((o, i) => (i === index ? { ...o, text } : o)) }));
  };

  const setCorrectOption = (index: number) => {
    setDraft((d) => ({
      ...d,
      options: d.options.map((o, i) => ({
        ...o,
        isCorrect: d.questionType === "MultipleResponse" ? (i === index ? !o.isCorrect : o.isCorrect) : i === index,
      })),
    }));
  };

  return (
    <Card className="p-5">
      <div className="flex items-start justify-between gap-4 mb-3">
        <div className="flex items-center gap-2 flex-wrap">
          <Badge tone={question.reviewStatus === "Approved" ? "success" : question.reviewStatus === "Rejected" ? "error" : "warning"}>
            {question.reviewStatus}
          </Badge>
          {question.isDuplicate && <Badge tone="error"><Copy size={12} className="inline mr-1" />Duplicate</Badge>}
          {isPublished && <Badge tone="brand">Published</Badge>}
        </div>
      </div>

      {question.validationIssues && (
        <div className="flex items-start gap-2 mb-4 p-3 bg-orange-50 border border-orange-200 rounded-md text-sm text-state-warning">
          <AlertTriangle size={16} className="mt-0.5 shrink-0" /> {question.validationIssues}
        </div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-3">
        <input
          value={draft.topic}
          onChange={(e) => setDraft((d) => ({ ...d, topic: e.target.value }))}
          disabled={isPublished}
          placeholder="Topic"
          className="h-10 px-3 rounded-md border border-border-subtle text-sm disabled:bg-bg-alt"
        />
        <input
          value={draft.subtopic ?? ""}
          onChange={(e) => setDraft((d) => ({ ...d, subtopic: e.target.value }))}
          disabled={isPublished}
          placeholder="Subtopic (optional)"
          className="h-10 px-3 rounded-md border border-border-subtle text-sm disabled:bg-bg-alt"
        />
        <select
          value={draft.difficulty}
          onChange={(e) => setDraft((d) => ({ ...d, difficulty: e.target.value }))}
          disabled={isPublished}
          className="h-10 px-3 rounded-md border border-border-subtle text-sm disabled:bg-bg-alt"
        >
          <option value="Easy">Easy</option>
          <option value="Medium">Medium</option>
          <option value="Hard">Hard</option>
        </select>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3">
        <select
          value={draft.questionType}
          onChange={(e) => setDraft((d) => ({ ...d, questionType: e.target.value }))}
          disabled={isPublished}
          className="h-10 px-3 rounded-md border border-border-subtle text-sm disabled:bg-bg-alt"
        >
          <option value="Choice">Single choice</option>
          <option value="MultipleResponse">Multiple response</option>
          <option value="Simulation">PBQ / simulation</option>
        </select>
        <label className="flex items-center gap-2 h-10 px-3 rounded-md border border-border-subtle text-sm">
          <input
            type="checkbox"
            checked={draft.requiresManualReview}
            onChange={(e) => setDraft((d) => ({ ...d, requiresManualReview: e.target.checked }))}
            disabled={isPublished}
          />
          Needs manual PBQ review
        </label>
      </div>

      {(draft.sourcePageStart || draft.images.length > 0) && (
        <div className="mb-3 text-xs text-text-secondary">
          {draft.sourcePageStart && <>PDF page {draft.sourcePageStart}{draft.sourcePageEnd && draft.sourcePageEnd !== draft.sourcePageStart ? `-${draft.sourcePageEnd}` : ""}</>}
        </div>
      )}

      {draft.images.length > 0 && (
        <div className="mb-4 flex flex-wrap gap-3">
          {draft.images.map((img) => <ImportedImage key={img.id} imageId={img.id} pageNumber={img.pageNumber} />)}
        </div>
      )}

      <textarea
        value={draft.prompt}
        onChange={(e) => setDraft((d) => ({ ...d, prompt: e.target.value }))}
        disabled={isPublished}
        rows={2}
        placeholder="Question text"
        className="w-full px-3 py-2 rounded-md border border-border-subtle text-sm mb-3 disabled:bg-bg-alt"
      />

      <div className="space-y-2 mb-3">
        {draft.options.map((opt, i) => (
          <div key={opt.id || i} className="flex items-center gap-2">
            <input
              type={draft.questionType === "MultipleResponse" ? "checkbox" : "radio"}
              checked={opt.isCorrect}
              onChange={() => setCorrectOption(i)}
              disabled={isPublished}
              aria-label={`Mark option ${i + 1} as correct`}
            />
            <input
              value={opt.text}
              onChange={(e) => updateOption(i, e.target.value)}
              disabled={isPublished}
              className={`flex-1 h-10 px-3 rounded-md border text-sm disabled:bg-bg-alt ${opt.isCorrect ? "border-state-success bg-green-50" : "border-border-subtle"}`}
            />
          </div>
        ))}
      </div>

      <textarea
        value={draft.explanation}
        onChange={(e) => setDraft((d) => ({ ...d, explanation: e.target.value }))}
        disabled={isPublished}
        rows={2}
        placeholder="Explanation"
        className="w-full px-3 py-2 rounded-md border border-border-subtle text-sm mb-3 disabled:bg-bg-alt"
      />

      <input
        value={draft.reference ?? ""}
        onChange={(e) => setDraft((d) => ({ ...d, reference: e.target.value }))}
        disabled={isPublished}
        placeholder="Reference (optional)"
        className="w-full h-10 px-3 rounded-md border border-border-subtle text-sm mb-4 disabled:bg-bg-alt"
      />

      {!isPublished && (
        <div className="space-y-3">
          {rejecting && (
            <div className="p-3 bg-bg-alt rounded-md">
              <textarea
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                rows={2}
                placeholder="Why is this question being rejected?"
                className="w-full px-3 py-2 rounded-md border border-border-subtle text-sm bg-white mb-2"
              />
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant="danger"
                  onClick={() => reject.mutate({ ids: [question.id], reason: rejectReason }, { onSuccess: () => setRejecting(false) })}
                  disabled={reject.isPending}
                >
                  Confirm reject
                </Button>
                <Button size="sm" variant="ghost" onClick={() => setRejecting(false)}>Cancel</Button>
              </div>
            </div>
          )}

          <div className="flex flex-wrap gap-2">
            {isDirty && (
              <Button size="sm" variant="secondary" onClick={handleSave} disabled={updateQuestion.isPending}>
                <Save size={14} /> {updateQuestion.isPending ? "Saving..." : "Save changes"}
              </Button>
            )}
            {question.reviewStatus !== "Approved" && (
              <Button
                size="sm"
                onClick={() => approve.mutate([question.id])}
                disabled={approve.isPending || isDirty}
                title={isDirty ? "Save your changes first" : undefined}
              >
                <CheckCircle2 size={14} /> Approve
              </Button>
            )}
            {question.reviewStatus !== "Rejected" && !rejecting && (
              <Button size="sm" variant="danger" onClick={() => setRejecting(true)}>
                <XCircle size={14} /> Reject
              </Button>
            )}
          </div>
        </div>
      )}
    </Card>
  );
}

export default function ImportReview() {
  const { jobId } = useParams<{ jobId: string }>();
  const { data: detail, isLoading } = useImportJobDetail(jobId);
  const approve = useApproveImportedQuestions();
  const publishVersion = usePublishVersion();
  const [publishResult, setPublishResult] = useState<{ succeeded: boolean; message: string } | null>(null);

  if (isLoading || !detail) return <p className="text-sm text-text-secondary">Loading import job...</p>;

  const { job, questions } = detail;
  const pendingIds = questions.filter((q) => q.reviewStatus === "PendingReview").length;
  const allApprovableIds = questions
    .filter((q) => q.reviewStatus === "PendingReview" && !q.validationIssues)
    .map((q) => q.id);

  const handlePublish = () => {
    setPublishResult(null);
    publishVersion.mutate(job.questionBankVersionId, {
      onSuccess: (data) =>
        setPublishResult({ succeeded: true, message: `Published ${data.questionsPublished} question(s) to the live question bank.` }),
      onError: (err: any) =>
        setPublishResult({ succeeded: false, message: err?.response?.data?.error ?? "Publish failed." }),
    });
  };

  return (
    <div className="space-y-6">
      <SectionHeading
        title={`Review: ${job.originalFileName}`}
        description={`${job.versionLabel} · ${questions.length} question(s) extracted`}
        action={
          <Link to="/admin/question-banks" className="text-sm font-semibold text-brand-primary hover:underline">
            Back to Question Banks
          </Link>
        }
      />

      {job.status === "Failed" && (
        <Card className="p-5 bg-red-50 border-red-200">
          <p className="text-sm text-state-error font-medium">Import failed: {job.errorMessage}</p>
        </Card>
      )}

      <Card className="p-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="text-sm text-text-secondary">
          <span className="font-semibold text-text-primary">{pendingIds}</span> pending review ·{" "}
          <span className="font-semibold text-state-success">{job.questionsApproved}</span> approved ·{" "}
          <span className="font-semibold text-state-error">{job.questionsRejected}</span> rejected
        </div>
        <div className="flex flex-wrap gap-2">
          {allApprovableIds.length > 0 && (
            <Button
              size="sm"
              variant="secondary"
              onClick={() => approve.mutate(allApprovableIds)}
              disabled={approve.isPending}
            >
              Approve all clean questions ({allApprovableIds.length})
            </Button>
          )}
          <Button
            size="sm"
            onClick={handlePublish}
            disabled={pendingIds > 0 || publishVersion.isPending}
            title={pendingIds > 0 ? "Every question must be approved or rejected first" : undefined}
          >
            <Rocket size={14} /> {publishVersion.isPending ? "Publishing..." : "Publish version"}
          </Button>
        </div>
      </Card>

      {publishResult && (
        <div className={`p-4 rounded-md border text-sm ${publishResult.succeeded ? "bg-green-50 border-green-200 text-state-success" : "bg-red-50 border-red-200 text-state-error"}`}>
          {publishResult.message}
        </div>
      )}

      <div className="space-y-4">
        {questions.map((q) => (
          <EditableQuestion key={q.id} question={q} />
        ))}
      </div>
    </div>
  );
}
