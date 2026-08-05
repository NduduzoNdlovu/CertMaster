import { Flame, ListChecks, Target, Trophy, Clock } from "lucide-react";
import { useAuth } from "../../context/AuthContext";
import { useDashboard } from "../../hooks/useDashboard";
import { Card, ProgressBar, SectionHeading, StatCard, Badge } from "../../components/ui/Primitives";
import { ActivityChart } from "../../components/charts/ActivityChart";
import { Link } from "react-router-dom";
import { Button } from "../../components/ui/Button";

export default function Dashboard() {
  const { user } = useAuth();
  const { data, isLoading } = useDashboard();

  if (isLoading || !data) {
    return <div className="text-text-secondary text-sm">Loading your dashboard...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-text-primary">Welcome back, {user?.fullName?.split(" ")[0]}</h1>
          <p className="text-sm text-text-secondary mt-1">Here's how your exam preparation is going.</p>
        </div>
        <Link to="/practice">
          <Button>Start a practice session</Button>
        </Link>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
        <Card className="p-5 lg:col-span-1 flex flex-col items-center justify-center text-center">
          <p className="text-sm text-text-secondary font-medium mb-2">Exam readiness</p>
          <p className="text-3xl font-extrabold text-brand-primary">{data.examReadinessScore}%</p>
          <div className="w-full mt-3">
            <ProgressBar percent={data.examReadinessScore} tone={data.examReadinessScore >= 70 ? "success" : data.examReadinessScore >= 40 ? "warning" : "error"} />
          </div>
        </Card>
        <StatCard label="Study streak" value={`${data.studyStreakDays} days`} icon={<Flame size={20} />} />
        <StatCard label="Questions answered" value={data.questionsAnswered.toLocaleString()} icon={<ListChecks size={20} />} />
        <StatCard label="Average score" value={`${data.averageScore}%`} icon={<Target size={20} />} />
        <StatCard label="Pass rate" value={`${data.passRate}%`} icon={<Trophy size={20} />} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2 p-5">
          <SectionHeading title="Daily activity" description="Minutes studied over the last 7 days" />
          <div className="h-64">
            <ActivityChart data={data.dailyActivity} />
          </div>
        </Card>

        <Card className="p-5">
          <SectionHeading title="This week" />
          <div className="space-y-4">
            <div className="flex items-center justify-between text-sm">
              <span className="flex items-center gap-2 text-text-secondary"><Clock size={16} /> Study time</span>
              <span className="font-semibold text-text-primary">{Math.round(data.studyTimeMinutesThisWeek / 60)}h {data.studyTimeMinutesThisWeek % 60}m</span>
            </div>
            <div className="flex items-center justify-between text-sm">
              <span className="text-text-secondary">Leaderboard position</span>
              <Badge tone="brand">#{data.leaderboardPosition}</Badge>
            </div>
          </div>
        </Card>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card className="p-5">
          <SectionHeading title="Weak topics" description="Focus your next practice session here" />
          <div className="space-y-4">
            {data.weakTopics.map((t) => (
              <div key={t.topic}>
                <div className="flex justify-between text-sm mb-1.5">
                  <span className="text-text-primary font-medium">{t.topic}</span>
                  <span className="text-text-secondary">{t.masteryPercent}%</span>
                </div>
                <ProgressBar percent={t.masteryPercent} tone="warning" />
              </div>
            ))}
          </div>
        </Card>

        <Card className="p-5">
          <SectionHeading title="Strong topics" description="Keep up the great work" />
          <div className="space-y-4">
            {data.strongTopics.map((t) => (
              <div key={t.topic}>
                <div className="flex justify-between text-sm mb-1.5">
                  <span className="text-text-primary font-medium">{t.topic}</span>
                  <span className="text-text-secondary">{t.masteryPercent}%</span>
                </div>
                <ProgressBar percent={t.masteryPercent} tone="success" />
              </div>
            ))}
          </div>
        </Card>
      </div>

      <Card className="p-5">
        <SectionHeading title="Recent exams" action={<Link to="/results" className="text-sm font-semibold text-brand-primary hover:underline">View all</Link>} />
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-text-secondary border-b border-border-subtle">
                <th className="py-2 pr-4 font-medium">Mode</th>
                <th className="py-2 pr-4 font-medium">Score</th>
                <th className="py-2 pr-4 font-medium">Result</th>
                <th className="py-2 pr-4 font-medium">Date</th>
              </tr>
            </thead>
            <tbody>
              {data.recentAttempts.map((a) => (
                <tr key={a.id} className="border-b border-border-subtle last:border-0">
                  <td className="py-3 pr-4 text-text-primary">{a.mode === "MockExam" ? "Mock Exam" : "Practice"}</td>
                  <td className="py-3 pr-4 text-text-primary font-medium">{a.score}%</td>
                  <td className="py-3 pr-4">
                    <Badge tone={a.passed ? "success" : "error"}>{a.passed ? "Pass" : "Fail"}</Badge>
                  </td>
                  <td className="py-3 pr-4 text-text-secondary">{new Date(a.startedAt).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
