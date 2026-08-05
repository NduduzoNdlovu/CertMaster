import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Bookmark, Flag, Shuffle, CheckCircle2, XCircle } from "lucide-react";
import { usePracticeQuestions, useBookmarkedIds, useToggleBookmark, useReportQuestion, useSubmitExam } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function Practice() {
  const [params] = useSearchParams();
  const certId = params.get("cert") ?? undefined;
  const { data: questions, isLoading } = usePracticeQuestions(certId);
  const { data: bookmarkedIds } = useBookmarkedIds();
  const toggleBookmark = useToggleBookmark();
  const reportQuestion = useReportQuestion();
  const submitExam = useSubmitExam();

  const [index, setIndex] = useState(0);
  const [selectedOption, setSelectedOption] = useState<string | null>(null);
  const [revealed, setRevealed] = useState(false);
  const [difficulty, setDifficulty] = useState<string>("All");
  const [reportOpen, setReportOpen] = useState(false);
  const [reportReason, setReportReason] = useState("");
  const [reportSent, setReportSent] = useState(false);
  const [questionStartedAt, setQuestionStartedAt] = useState(() => Date.now());

  if (isLoading || !questions) {
    return <p className="text-sm text-text-secondary">Loading practice questions...</p>;
  }

  const filtered = difficulty === "All" ? questions : questions.filter((q) => q.difficulty === difficulty);

  if (filtered.length === 0) {
    return (
      <Card className="p-8 max-w-lg mx-auto text-center">
        <p className="text-sm text-text-secondary">
          {questions.length === 0
            ? "No published questions yet for this certification. An administrator needs to import and publish a question bank first."
            : "No questions match this difficulty filter yet — try a different one."}
        </p>
      </Card>
    );
  }

  const question = filtered[index % filtered.length];
  const isBookmarked = question ? bookmarkedIds?.has(question.id) ?? false : false;

  const isCorrect = (optionId: string) => question.options.find((o) => o.id === optionId)?.isCorrect;

  const handleSelect = (optionId: string) => {
    if (revealed || !question) return;
    setSelectedOption(optionId);
    setRevealed(true);

    submitExam.mutate({
      certificationId: question.certificationId,
      mode: "Practice",
      durationSeconds: Math.max(1, Math.round((Date.now() - questionStartedAt) / 1000)),
      answers: [{ questionId: question.id, selectedOptionId: optionId, wasFlaggedForReview: false }],
    });
  };

  const nextQuestion = () => {
    setIndex((i) => (i + 1) % filtered.length);
    setSelectedOption(null);
    setRevealed(false);
    setReportOpen(false);
    setReportSent(false);
    setReportReason("");
    setQuestionStartedAt(Date.now());
  };

  const shuffle = () => {
    setIndex(Math.floor(Math.random() * filtered.length));
    setSelectedOption(null);
    setRevealed(false);
    setQuestionStartedAt(Date.now());
  };

  const handleReportSubmit = () => {
    if (!question) return;
    reportQuestion.mutate(
      { questionId: question.id, reason: reportReason || "No reason provided" },
      { onSuccess: () => setReportSent(true) }
    );
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <SectionHeading title="Practice mode" description="Unlimited attempts with instant feedback and explanations" />
        <div className="flex items-center gap-2">
          <select
            value={difficulty}
            onChange={(e) => { setDifficulty(e.target.value); setIndex(0); setRevealed(false); setSelectedOption(null); }}
            className="h-11 px-3 rounded-md border border-border-subtle bg-white text-sm"
          >
            <option>All</option>
            <option>Easy</option>
            <option>Medium</option>
            <option>Hard</option>
          </select>
          <Button variant="secondary" size="sm" onClick={shuffle}>
            <Shuffle size={16} /> Shuffle
          </Button>
        </div>
      </div>

      <Card className="p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Badge tone="brand">{question?.topic}</Badge>
            <Badge tone={question?.difficulty === "Hard" ? "error" : question?.difficulty === "Medium" ? "warning" : "success"}>
              {question?.difficulty}
            </Badge>
          </div>
          <div className="flex items-center gap-1">
            <button
              onClick={() => question && toggleBookmark.mutate(question.id)}
              className={`h-10 w-10 flex items-center justify-center rounded-md hover:bg-bg-alt ${isBookmarked ? "text-brand-primary" : "text-text-secondary hover:text-brand-primary"}`}
              aria-label="Bookmark question"
            >
              <Bookmark size={18} className={isBookmarked ? "fill-brand-primary" : ""} />
            </button>
            <button
              onClick={() => setReportOpen((v) => !v)}
              className="h-10 w-10 flex items-center justify-center rounded-md text-text-secondary hover:bg-bg-alt hover:text-state-error"
              aria-label="Report question"
            >
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
                <textarea
                  value={reportReason}
                  onChange={(e) => setReportReason(e.target.value)}
                  rows={2}
                  placeholder="e.g. the marked correct answer looks wrong, a typo, an outdated reference..."
                  className="w-full px-3 py-2 rounded-md border border-border-subtle bg-white text-sm mb-2"
                />
                <Button size="sm" onClick={handleReportSubmit} disabled={reportQuestion.isPending}>
                  {reportQuestion.isPending ? "Sending..." : "Submit report"}
                </Button>
              </>
            )}
          </div>
        )}

        <p className="text-base font-medium text-text-primary mb-5">{question.prompt}</p>

        <div className="space-y-3">
          {question.options.map((opt) => {
            const showCorrect = revealed && opt.isCorrect;
            const showIncorrect = revealed && selectedOption === opt.id && !opt.isCorrect;
            return (
              <button
                key={opt.id}
                onClick={() => handleSelect(opt.id)}
                className={`w-full text-left px-4 py-3 rounded-md border text-sm flex items-center justify-between transition-colors min-h-[44px]
                  ${showCorrect ? "border-state-success bg-green-50" : showIncorrect ? "border-state-error bg-red-50" : "border-border-subtle hover:bg-bg-alt"}
                `}
              >
                <span className="text-text-primary">{opt.text}</span>
                {showCorrect && <CheckCircle2 size={18} className="text-state-success shrink-0" />}
                {showIncorrect && <XCircle size={18} className="text-state-error shrink-0" />}
              </button>
            );
          })}
        </div>

        {revealed && (
          <div className="mt-5 p-4 rounded-md bg-bg-alt border border-border-subtle">
            <p className="text-sm font-semibold text-text-primary mb-1">
              {selectedOption && isCorrect(selectedOption) ? "Correct!" : "Not quite."}
            </p>
            <p className="text-sm text-text-secondary">{question.explanation}</p>
            {question.reference && <p className="text-xs text-text-secondary mt-2">Reference: {question.reference}</p>}
          </div>
        )}

        <div className="flex justify-end mt-5">
          <Button onClick={nextQuestion} disabled={!revealed}>Next question</Button>
        </div>
      </Card>
    </div>
  );
}
