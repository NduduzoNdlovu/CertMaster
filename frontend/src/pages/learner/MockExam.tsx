import { useEffect, useMemo, useState } from "react";
import { Clock, Flag, WifiOff } from "lucide-react";
import { useSearchParams } from "react-router-dom";
import { useCompleteExam, useSaveExamAnswer, useStartExam } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";
import type { ExamSession, ExamSubmitResult } from "../../types";

const formatTime = (s: number) => `${Math.floor(s / 60).toString().padStart(2, "0")}:${(s % 60).toString().padStart(2, "0")}`;

export default function MockExam() {
  const [params] = useSearchParams(); const certId = params.get("cert") ?? "";
  const start = useStartExam(); const save = useSaveExamAnswer(); const complete = useCompleteExam();
  const [session, setSession] = useState<ExamSession>(); const [current, setCurrent] = useState(0);
  const [answers, setAnswers] = useState<Record<string, string>>({}); const [flagged, setFlagged] = useState<Set<string>>(new Set());
  const [secondsLeft, setSecondsLeft] = useState(0); const [result, setResult] = useState<ExamSubmitResult>(); const [offline, setOffline] = useState(!navigator.onLine);
  const storageKey = session ? `certmaster-exam-${session.attemptId}` : "";

  const submit = async () => { if (!session || complete.isPending || result) return; try { setResult(await complete.mutateAsync(session.attemptId)); localStorage.removeItem(storageKey); } catch { /* keep state for retry */ } };
  useEffect(() => { const online = () => setOffline(false); const off = () => setOffline(true); addEventListener("online", online); addEventListener("offline", off); return () => { removeEventListener("online", online); removeEventListener("offline", off); }; }, []);
  useEffect(() => { if (!session || result) return; const tick = () => { const left = Math.max(0, Math.ceil((new Date(session.expiresAtUtc).getTime() - Date.now()) / 1000)); setSecondsLeft(left); if (left === 0) void submit(); }; tick(); const id = setInterval(tick, 1000); return () => clearInterval(id); }, [session, result]); // eslint-disable-line react-hooks/exhaustive-deps
  useEffect(() => { if (!session) return; const saved = localStorage.getItem(storageKey); const local = saved ? JSON.parse(saved) as { answers: Record<string,string>; flagged: string[] } : undefined; setAnswers({ ...Object.fromEntries(session.questions.filter(q => q.selectedOptionId).map(q => [q.id, q.selectedOptionId!])), ...(local?.answers ?? {}) }); setFlagged(new Set([ ...session.questions.filter(q => q.wasFlaggedForReview).map(q => q.id), ...(local?.flagged ?? []) ])); }, [session, storageKey]);
  useEffect(() => { if (session) localStorage.setItem(storageKey, JSON.stringify({ answers, flagged: [...flagged] })); }, [answers, flagged, session, storageKey]);
  useEffect(() => { if (!session || offline) return; Object.entries(answers).forEach(([questionId, selectedOptionId]) => save.mutate({ attemptId: session.attemptId, questionId, selectedOptionId, wasFlaggedForReview: flagged.has(questionId) })); }, [offline]); // eslint-disable-line react-hooks/exhaustive-deps

  const saveAnswer = (questionId: string, selectedOptionId: string | null, isFlagged: boolean) => {
    if (selectedOptionId) setAnswers(a => ({ ...a, [questionId]: selectedOptionId }));
    save.mutate({ attemptId: session!.attemptId, questionId, selectedOptionId, wasFlaggedForReview: isFlagged });
  };
  const begin = async () => { if (!certId) return; try { setSession(await start.mutateAsync({ certificationId: certId, mode: "MockExam" })); } catch { /* displayed below */ } };
  const question = useMemo(() => session?.questions[current], [session, current]);

  if (!session) return <Card className="p-8 max-w-2xl mx-auto text-center"><h1 className="text-xl font-bold mb-2">Mock Exam</h1><p className="text-sm text-text-secondary mb-6">Start a new exam or resume your unfinished exam. Duration, question count and pass mark come from the selected certification.</p><Button onClick={begin} disabled={!certId || start.isPending}>{start.isPending ? "Preparing..." : "Start or resume exam"}</Button>{start.isError && <p className="text-state-error mt-4">Could not start the exam. Confirm that the certification has published questions.</p>}</Card>;
  if (result) return <Card className="p-8 max-w-2xl mx-auto text-center"><Badge tone={result.passed ? "success" : "error"}>{result.passed ? "PASS" : "FAIL"}</Badge><h1 className="text-3xl font-bold mt-4">{result.score}%</h1><p>{result.correctCount} of {result.totalQuestions} correct</p></Card>;
  if (!question) return <p>No questions are available.</p>;

  return <div className="grid lg:grid-cols-4 gap-6"><div className="lg:col-span-3 space-y-4">
    <Card className="p-4 flex justify-between sticky top-[82px] z-10"><SectionHeading title={`Question ${current + 1} of ${session.questions.length}`} /><div className="flex gap-3">{offline && <span className="text-state-error flex gap-1"><WifiOff size={18}/>Offline — answers kept locally</span>}<span className="flex gap-1 font-semibold"><Clock size={18}/>{formatTime(secondsLeft)}</span></div></Card>
    <Card className="p-6"><div className="flex justify-between"><Badge tone="brand">{question.topic}</Badge><button onClick={() => { const next = new Set(flagged); next.has(question.id) ? next.delete(question.id) : next.add(question.id); setFlagged(next); saveAnswer(question.id, answers[question.id] ?? null, next.has(question.id)); }}><Flag className={flagged.has(question.id) ? "text-brand-accent" : "text-text-secondary"}/></button></div><p className="font-medium my-5">{question.prompt}</p><div className="space-y-3">{question.options.map(o => <button key={o.id} onClick={() => saveAnswer(question.id, o.id, flagged.has(question.id))} className={`w-full text-left p-4 rounded-md border ${answers[question.id] === o.id ? "border-brand-primary bg-red-50" : "border-border-subtle"}`}>{o.text}</button>)}</div></Card>
    <div className="flex justify-between"><Button variant="secondary" disabled={current === 0} onClick={() => setCurrent(c => c - 1)}>Previous</Button>{current === session.questions.length - 1 ? <Button variant="danger" onClick={() => void submit()} disabled={complete.isPending}>{complete.isPending ? "Submitting..." : "Submit exam"}</Button> : <Button onClick={() => setCurrent(c => c + 1)}>Next</Button>}</div>{complete.isError && <p className="text-state-error">Submission failed. Your answers are safe; reconnect and try again.</p>}
  </div><Card className="p-4 h-fit"><SectionHeading title="Navigator"/><div className="grid grid-cols-5 gap-2">{session.questions.map((q, i) => <button key={q.id} onClick={() => setCurrent(i)} className={`h-9 rounded border ${i === current ? "bg-brand-primary text-white" : answers[q.id] ? "bg-green-50" : ""} ${flagged.has(q.id) ? "ring-2 ring-brand-accent" : ""}`}>{i + 1}</button>)}</div></Card></div>;
}
