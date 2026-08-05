import { useEffect, useState } from "react";
import { Flag, Clock } from "lucide-react";
import { usePracticeQuestions, useSubmitExam } from "../../hooks/useApiData";
import { useSearchParams } from "react-router-dom";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";
import type { ExamSubmitResult } from "../../types";

function formatTime(seconds: number) {
  const m = Math.floor(seconds / 60).toString().padStart(2, "0");
  const s = (seconds % 60).toString().padStart(2, "0");
  return `${m}:${s}`;
}

const EXAM_DURATION_SECONDS = 90 * 60;

export default function MockExam() {
  const [params] = useSearchParams();
  const certId = params.get("cert") ?? undefined;
  const { data: questions } = usePracticeQuestions(certId);
  const submitExam = useSubmitExam();

  const [started, setStarted] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [current, setCurrent] = useState(0);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [flagged, setFlagged] = useState<Set<string>>(new Set());
  const [secondsLeft, setSecondsLeft] = useState(EXAM_DURATION_SECONDS);
  const [result, setResult] = useState<ExamSubmitResult | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    if (!started || submitted) return;
    const timer = setInterval(() => {
      setSecondsLeft((s) => {
        if (s <= 1) {
          clearInterval(timer);
          handleSubmit();
          return 0;
        }
        return s - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [started, submitted]);

  if (!questions) return <p className="text-sm text-text-secondary">Loading exam...</p>;

  if (questions.length === 0) {
    return (
      <Card className="p-8 max-w-lg mx-auto text-center">
        <p className="text-sm text-text-secondary">
          No published questions yet for this certification. An administrator needs to import and publish a
          question bank before a mock exam can be taken.
        </p>
      </Card>
    );
  }

  const handleSubmit = () => {
    if (submitted || submitExam.isPending) return;
    setSubmitted(true);
    setSubmitError(null);

    submitExam.mutate(
      {
        certificationId: certId ?? questions[0]?.certificationId ?? "",
        mode: "MockExam",
        durationSeconds: EXAM_DURATION_SECONDS - secondsLeft,
        answers: questions.map((q) => ({
          questionId: q.id,
          selectedOptionId: answers[q.id] ?? null,
          wasFlaggedForReview: flagged.has(q.id),
        })),
      },
      {
        onSuccess: (data) => setResult(data),
        onError: () => setSubmitError("Couldn't submit your exam. Your answers are still on this page — try again."),
      }
    );
  };

  if (!started) {
    return (
      <Card className="p-8 max-w-2xl mx-auto text-center">
        <h1 className="text-xl font-bold text-text-primary mb-2">CompTIA Mock Exam Simulation</h1>
        <p className="text-sm text-text-secondary mb-6">
          {questions.length} questions &middot; 90 minutes &middot; Passing score 65%. Once started, the timer cannot be paused.
        </p>
        <Button onClick={() => setStarted(true)}>Start mock exam</Button>
      </Card>
    );
  }

  if (submitted) {
    if (submitError) {
      return (
        <Card className="p-8 max-w-2xl mx-auto text-center">
          <p className="text-sm text-state-error mb-4">{submitError}</p>
          <Button onClick={handleSubmit}>Retry submission</Button>
        </Card>
      );
    }

    if (!result) {
      return <p className="text-sm text-text-secondary text-center">Submitting your exam...</p>;
    }

    return (
      <Card className="p-8 max-w-2xl mx-auto text-center">
        <Badge tone={result.passed ? "success" : "error"}>{result.passed ? "PASS" : "FAIL"}</Badge>
        <h1 className="text-3xl font-extrabold text-text-primary mt-4">{result.score}%</h1>
        <p className="text-sm text-text-secondary mt-2">
          {result.correctCount} of {result.totalQuestions} correct
        </p>
        <div className="grid grid-cols-2 gap-4 mt-6 text-left">
          <div className="p-4 rounded-md bg-bg-alt">
            <p className="text-xs text-text-secondary">Time used</p>
            <p className="font-semibold text-text-primary">{formatTime(EXAM_DURATION_SECONDS - secondsLeft)}</p>
          </div>
          <div className="p-4 rounded-md bg-bg-alt">
            <p className="text-xs text-text-secondary">Flagged questions</p>
            <p className="font-semibold text-text-primary">{flagged.size}</p>
          </div>
        </div>
        <Button className="mt-6" onClick={() => window.location.reload()}>Return to certifications</Button>
      </Card>
    );
  }

  const question = questions[current % questions.length];

  return (
    <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
      <div className="lg:col-span-3 space-y-4">
        <Card className="p-4 flex items-center justify-between sticky top-[82px] z-10">
          <SectionHeading title={`Question ${current + 1} of ${questions.length}`} />
          <div className="flex items-center gap-2 text-brand-primary font-semibold">
            <Clock size={18} />
            {formatTime(secondsLeft)}
          </div>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between mb-4">
            <Badge tone="brand">{question.topic}</Badge>
            <button
              onClick={() =>
                setFlagged((prev) => {
                  const next = new Set(prev);
                  next.has(question.id) ? next.delete(question.id) : next.add(question.id);
                  return next;
                })
              }
              className={`h-10 w-10 flex items-center justify-center rounded-md ${flagged.has(question.id) ? "text-brand-accent" : "text-text-secondary hover:bg-bg-alt"}`}
              aria-label="Flag for review"
            >
              <Flag size={18} />
            </button>
          </div>
          <p className="text-base font-medium text-text-primary mb-5">{question.prompt}</p>
          <div className="space-y-3">
            {question.options.map((opt) => (
              <button
                key={opt.id}
                onClick={() => setAnswers((a) => ({ ...a, [question.id]: opt.id }))}
                className={`w-full text-left px-4 py-3 rounded-md border text-sm min-h-[44px] transition-colors ${
                  answers[question.id] === opt.id ? "border-brand-primary bg-red-50" : "border-border-subtle hover:bg-bg-alt"
                }`}
              >
                {opt.text}
              </button>
            ))}
          </div>
        </Card>

        <div className="flex justify-between">
          <Button variant="secondary" disabled={current === 0} onClick={() => setCurrent((c) => c - 1)}>
            Previous
          </Button>
          {current === questions.length - 1 ? (
            <Button variant="danger" onClick={handleSubmit} disabled={submitExam.isPending}>
              {submitExam.isPending ? "Submitting..." : "Submit exam"}
            </Button>
          ) : (
            <Button onClick={() => setCurrent((c) => c + 1)}>Next</Button>
          )}
        </div>
      </div>

      <Card className="p-4 h-fit">
        <SectionHeading title="Navigator" />
        <div className="grid grid-cols-5 gap-2">
          {questions.map((q, i) => {
            const answered = Boolean(answers[q.id]);
            const isFlagged = flagged.has(q.id);
            return (
              <button
                key={q.id}
                onClick={() => setCurrent(i)}
                className={`h-9 w-9 rounded-md text-xs font-semibold flex items-center justify-center border
                  ${i === current ? "border-brand-primary bg-brand-primary text-white" : answered ? "border-state-success bg-green-50 text-state-success" : "border-border-subtle text-text-secondary"}
                  ${isFlagged ? "ring-2 ring-brand-accent" : ""}
                `}
              >
                {i + 1}
              </button>
            );
          })}
        </div>
      </Card>
    </div>
  );
}
