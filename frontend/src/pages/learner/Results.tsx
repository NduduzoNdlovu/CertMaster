import { mockExamAttempts } from "../../lib/mockData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";

export default function Results() {
  return (
    <div className="space-y-6">
      <SectionHeading title="My Results" description="Every practice session and mock exam you've completed" />
      <Card className="p-0 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-text-secondary bg-bg-alt">
              <th className="py-3 px-5 font-medium">Mode</th>
              <th className="py-3 px-5 font-medium">Questions</th>
              <th className="py-3 px-5 font-medium">Correct</th>
              <th className="py-3 px-5 font-medium">Score</th>
              <th className="py-3 px-5 font-medium">Result</th>
              <th className="py-3 px-5 font-medium">Date</th>
            </tr>
          </thead>
          <tbody>
            {mockExamAttempts.map((a) => (
              <tr key={a.id} className="border-t border-border-subtle">
                <td className="py-3 px-5 text-text-primary">{a.mode === "MockExam" ? "Mock Exam" : "Practice"}</td>
                <td className="py-3 px-5 text-text-secondary">{a.totalQuestions}</td>
                <td className="py-3 px-5 text-text-secondary">{a.correctCount}</td>
                <td className="py-3 px-5 font-semibold text-text-primary">{a.score}%</td>
                <td className="py-3 px-5"><Badge tone={a.passed ? "success" : "error"}>{a.passed ? "Pass" : "Fail"}</Badge></td>
                <td className="py-3 px-5 text-text-secondary">{new Date(a.startedAt).toLocaleDateString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
