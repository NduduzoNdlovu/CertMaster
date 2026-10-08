import { Link, useParams } from "react-router-dom";
import { useAdminResult } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";

export default function ResultDetails() {
  const { id } = useParams<{ id: string }>(); const { data, isLoading } = useAdminResult(id);
  if (isLoading) return <p>Loading result...</p>; if (!data) return <p className="text-state-error">Result not found.</p>;
  const a = data.attempt;
  return <div className="space-y-6"><SectionHeading title="Result details" description={`${a.userName} · ${a.certificationName}`} action={<Link to="/admin/results" className="text-sm font-semibold text-brand-primary hover:underline">Back to results</Link>} />
    <Card className="p-5 grid grid-cols-2 md:grid-cols-4 gap-4"><div><p className="text-xs text-text-secondary">Score</p><p className="text-2xl font-bold">{a.score}%</p></div><div><p className="text-xs text-text-secondary">Correct</p><p className="text-2xl font-bold">{a.correctCount}/{a.totalQuestions}</p></div><div><p className="text-xs text-text-secondary">Mode</p><p className="font-semibold mt-2">{a.mode}</p></div><div><p className="text-xs text-text-secondary">Outcome</p><div className="mt-2"><Badge tone={a.passed ? "success" : "error"}>{a.passed ? "Passed" : "Failed"}</Badge></div></div></Card>
    <Card className="p-0 overflow-x-auto"><table className="w-full text-sm"><thead><tr className="text-left bg-bg-alt"><th className="p-3">Question</th><th>Correct</th><th>Flagged</th></tr></thead><tbody>{data.answers.map((q,i)=><tr key={`${q.questionId}-${i}`} className="border-t"><td className="p-3 max-w-3xl">{q.prompt}</td><td><Badge tone={q.isCorrect ? "success" : "error"}>{q.isCorrect ? "Correct" : "Incorrect"}</Badge></td><td>{q.wasFlaggedForReview ? "Yes" : "No"}</td></tr>)}</tbody></table></Card>
  </div>;
}
