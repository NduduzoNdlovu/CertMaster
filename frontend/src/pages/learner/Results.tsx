import { useState } from "react";
import { useMyResult, useMyResults } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";

export default function Results() {
  const { data = [], isLoading, isError } = useMyResults();
  const [selectedId, setSelectedId] = useState<string>();
  const detail = useMyResult(selectedId);
  if (isLoading) return <p className="text-sm text-text-secondary">Loading results...</p>;
  if (isError) return <p className="text-sm text-state-error">Could not load your results.</p>;
  return <div className="space-y-6">
    <SectionHeading title="My Results" description="Your completed practice sessions and mock exams" />
    <Card className="p-0 overflow-x-auto"><table className="w-full text-sm"><thead><tr className="text-left text-text-secondary bg-bg-alt">
      <th className="py-3 px-5">Certification</th><th className="py-3 px-5">Mode</th><th className="py-3 px-5">Correct</th><th className="py-3 px-5">Score</th><th className="py-3 px-5">Result</th><th className="py-3 px-5">Date</th>
    </tr></thead><tbody>{data.map(a => <tr key={a.id} className="border-t border-border-subtle cursor-pointer hover:bg-bg-alt" onClick={() => setSelectedId(a.id)}>
      <td className="py-3 px-5">{(a as typeof a & { certificationName?: string }).certificationName ?? a.certificationId}</td><td className="py-3 px-5">{a.mode === "MockExam" ? "Mock Exam" : "Practice"}</td><td className="py-3 px-5">{a.correctCount}/{a.totalQuestions}</td><td className="py-3 px-5 font-semibold">{a.score}%</td><td className="py-3 px-5"><Badge tone={a.passed ? "success" : "error"}>{a.passed ? "Pass" : "Fail"}</Badge></td><td className="py-3 px-5">{new Date(a.completedAt ?? a.startedAt).toLocaleDateString()}</td>
    </tr>)}</tbody></table>{data.length === 0 && <p className="p-6 text-sm text-text-secondary">No completed exams yet.</p>}</Card>
    {selectedId && <Card className="p-5"><div className="flex justify-between"><SectionHeading title="Answer review" /><button onClick={() => setSelectedId(undefined)}>Close</button></div>
      {detail.isLoading ? <p>Loading answers...</p> : detail.data?.answers.map((a, i) => <div key={a.questionId} className="border-t border-border-subtle py-4"><p className="font-medium">{i + 1}. {a.prompt}</p><p className="text-sm mt-2">Your answer: {a.selectedOption ?? "Not answered"} <Badge tone={a.isCorrect ? "success" : "error"}>{a.isCorrect ? "Correct" : "Incorrect"}</Badge></p>{!a.isCorrect && <p className="text-sm text-text-secondary">Correct answer: {a.correctOption}</p>}<p className="text-sm text-text-secondary mt-1">{a.explanation}</p></div>)}</Card>}
  </div>;
}
