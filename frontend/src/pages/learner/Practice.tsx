import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Bookmark, Flag, Shuffle, CheckCircle2, XCircle } from "lucide-react";
import {
  useCertifications,
  useBookmarkedIds,
  useToggleBookmark,
  useReportQuestion,
  useStartExam,
  useSubmitPracticeAnswer,
  useCompleteExam,
} from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";
import type { ExamSession, ExamSubmitResult } from "../../types";

export default function Practice() {
  const [params] = useSearchParams();
  const initialCertId = params.get("cert") ?? "";
  const { data: certifications = [] } = useCertifications();
  const [certId, setCertId] = useState(initialCertId);
  const [session, setSession] = useState<ExamSession>();
  const [current, setCurrent] = useState(0);
  const [result, setResult] = useState<ExamSubmitResult>();
  const [selectedOption, setSelectedOption] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{ isCorrect: boolean; correctOption?: string; explanation: string } | null>(null);
  const [flagged, setFlagged] = useState<Set<string>>(new Set());
  const [reportOpen, setReportOpen] = useState(false);
  const [reportReason, setReportReason] = useState("");
  const [reportSent, setReportSent] = useState(false);

  const { data: bookmarkedIds } = useBookmarkedIds();
  const toggleBookmark = useToggleBookmark();
  const reportQuestion = useReportQuestion();
  const start = useStartExam();
  const submitAnswer = useSubmitPracticeAnswer();
  const complete = useCompleteExam();

  const question = useMemo(() => session?.questions[current], [session, current]);
  const selectedCertification = certifications.find((c) => c.id === certId);

  const begin = async () => {
    if (!certId) return;
    try {
      const next = await start.mutateAsync({ certificationId: certId, mode: "Practice" });
      setSession(next);
      setCurrent(0);
      setResult(undefined);
      setSelectedOption(next.questions[0]?.selectedOptionId ?? null);
      setFeedback(null);
      setFlagged(new Set(next.questions.filter(q => q.wasFlaggedForReview).map(q => q.id)));
    } catch {
      // Error is displayed below.
    }
  };

  const selectAnswer = async (optionId: string) => {
    if (!session || !question || feedback || submitAnswer.isPending) return;
    setSelectedOption(optionId);

    try {
      const response = await submitAnswer.mutateAsync({
        attemptId: session.attemptId,
        questionId: question.id,
        selectedOptionId: optionId,
        wasFlaggedForReview: flagged.has(question.id),
      });
      setFeedback(response);
    } catch {
      setSelectedOption(null);
    }
  };

  const toggleFlag = () => {
    if (!question || !session) return;
    const next = new Set(flagged);
    if (next.has(question.id)) next.delete(question.id);
    else next.add(question.id);
    setFlagged(next);
    if (selectedOption) {
      void submitAnswer.mutateAsync({
        attemptId: session.attemptId,
        questionId: question.id,
        selectedOptionId: selectedOption,
        wasFlaggedForReview: next.has(question.id),
      });
    }
  };

  const nextQuestion = () => {
    if (!session || current >= session.questions.length - 1) return;
    const nextIndex = current + 1;
    const nextQuestion = session.questions[nextIndex];
    setCurrent(nextIndex);
    setSelectedOption(nextQuestion.selectedOptionId ?? null);
    setFeedback(null);
    setReportOpen(false);
    setReportSent(false);
    setReportReason("");
  };

  const finish = async () => {
    if (!session || complete.isPending) return;
    try {
      setResult(await complete.mutateAsync(session.attemptId));
    } catch {
      // Keep the session visible so the learner can retry.
    }
  };

  const shuffle = () => {
    if (!session) return;
    setCurrent(Math.floor(Math.random() * session.questions.length));
    setFeedback(null);
    setSelectedOption(session.questions[current]?.selectedOptionId ?? null);
  };

  const handleReportSubmit = () => {
    if (!question) return;
    reportQuestion.mutate(
      { questionId: question.id, reason: reportReason || "No reason provided" },
      { onSuccess: () => setReportSent(true) }
    );
  };

  if (!session) {
    return (
      <div className="space-y-6">
        <SectionHeading title="Practice" description="Work through a focused set of questions with instant feedback." />
        <Card className="p-6 max-w-2xl">
          <label className="block text-sm font-medium text-text-primary mb-2">Certification</label>
          <select
            value={certId}
            onChange={(e) => setCertId(e.target.value)}
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm"
          >
            <option value="">Choose a certification</option>
            {certifications.map(cert => <option key={cert.id} value={cert.id}>{cert.name} ({cert.code})</option>)}
          </select>
          {selectedCertification && (
            <p className="text-sm text-text-secondary mt-3">
              You will receive up to 20 published questions from {selectedCertification.name}. Your progress is saved while the session is active.
            </p>
          )}
          <Button className="mt-5" onClick={begin} disabled={!certId || start.isPending}>
            {start.isPending ? "Preparing..." : "Start practice"}
          </Button>
          {start.isError && <p className="text-sm text-state-error mt-3">Could not start practice. Make sure this certification has published questions.</p>}
        </Card>
      </div>
    );
  }

  if (result) {
    return (
      <Card className="p-8 max-w-2xl mx-auto text-center">
        <Badge tone={result.passed ? "success" : "warning"}>Practice complete</Badge>
        <h1 className="text-3xl font-bold mt-4">{result.score}%</h1>
        <p className="text-text-secondary mt-1">{result.correctCount} of {result.totalQuestions} correct</p>
        <Button className="mt-6" onClick={() => { setSession(undefined); setResult(undefined); }}>
          Start another practice session
        </Button>
      </Card>
    );
  }

  if (!question) return <p className="text-sm text-state-error">This practice session has no questions.</p>;

  const isBookmarked = bookmarkedIds?.has(question.id) ?? false;
  const isLast = current === session.questions.length - 1;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <SectionHeading title={`Practice — ${session.certificationName}`} description={`Question ${current + 1} of ${session.questions.length}`} />
        <Button variant="secondary" size="sm" onClick={shuffle}><Shuffle size={16} /> Shuffle</Button>
      </div>

      <Card className="p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Badge tone="brand">{question.topic}</Badge>
            <Badge>Practice</Badge>
          </div>
          <div className="flex items-center gap-1">
            <button onClick={toggleFlag} className={`h-10 w-10 flex items-center justify-center rounded-md hover:bg-bg-alt ${flagged.has(question.id) ? "text-brand-accent" : "text-text-secondary"}`} aria-label="Flag question">
              <Flag size={18} />
            </button>
            <button
              onClick={() => toggleBookmark.mutate(question.id)}
              className={`h-10 w-10 flex items-center justify-center rounded-md hover:bg-bg-alt ${isBookmarked ? "text-brand-primary" : "text-text-secondary"}`}
              aria-label="Bookmark question"
            >
              <Bookmark size={18} className={isBookmarked ? "fill-brand-primary" : ""} />
            </button>
            <button onClick={() => setReportOpen(v => !v)} className="h-10 w-10 flex items-center justify-center rounded-md text-text-secondary hover:bg-bg-alt hover:text-state-error" aria-label="Report question">
              <Flag size={18} />
            </button>
          </div>
        </div>

        {reportOpen && (
          <div className="mb-5 p-4 rounded-md bg-bg-alt border border-border-subtle">
            {reportSent ? (
              <p className="text-sm text-state-success font-medium">Thanks — our content team will review this question.</p>
            ) : (
              <>
                <label className="block text-sm font-medium text-text-primary mb-1.5">What's wrong with this question?</label>
                <textarea value={reportReason} onChange={e => setReportReason(e.target.value)} rows={2} className="w-full px-3 py-2 rounded-md border border-border-subtle bg-white text-sm mb-2" />
                <Button size="sm" onClick={handleReportSubmit} disabled={reportQuestion.isPending}>{reportQuestion.isPending ? "Sending..." : "Submit report"}</Button>
              </>
            )}
          </div>
        )}

        <p className="text-base font-medium text-text-primary mb-5">{question.prompt}</p>

        <div className="space-y-3">
          {question.options.map(opt => {
            const correct = feedback?.isCorrect && selectedOption === opt.id;
            const selectedWrong = feedback && selectedOption === opt.id && !feedback.isCorrect;
            return (
              <button
                key={opt.id}
                onClick={() => void selectAnswer(opt.id)}
                disabled={Boolean(feedback) || submitAnswer.isPending}
                className={`w-full text-left px-4 py-3 rounded-md border text-sm flex items-center justify-between min-h-[44px] ${
                  correct ? "border-state-success bg-green-50" :
                  selectedWrong ? "border-state-error bg-red-50" :
                  "border-border-subtle hover:bg-bg-alt"
                }`}
              >
                <span>{opt.text}</span>
                {correct && <CheckCircle2 size={18} className="text-state-success" />}
                {selectedWrong && <XCircle size={18} className="text-state-error" />}
              </button>
            );
          })}
        </div>

        {feedback && (
          <div className="mt-5 p-4 rounded-md bg-bg-alt border border-border-subtle">
            <p className="text-sm font-semibold">{feedback.isCorrect ? "Correct!" : `Not quite. Correct answer: ${feedback.correctOption ?? "See explanation."}`}</p>
            <p className="text-sm text-text-secondary mt-1">{feedback.explanation}</p>
          </div>
        )}

        <div className="flex justify-between mt-5">
          <Button variant="secondary" disabled={current === 0} onClick={() => {
            const i = current - 1;
            setCurrent(i);
            setSelectedOption(session.questions[i]?.selectedOptionId ?? null);
            setFeedback(null);
          }}>Previous</Button>
          {isLast ? (
            <Button onClick={() => void finish()} disabled={!feedback || complete.isPending}>
              {complete.isPending ? "Finishing..." : "Finish practice"}
            </Button>
          ) : (
            <Button onClick={nextQuestion} disabled={!feedback}>Next question</Button>
          )}
        </div>
        {complete.isError && <p className="text-sm text-state-error mt-3">Could not save the result. Your answers are still saved; please try again.</p>}
      </Card>
    </div>
  );
}
