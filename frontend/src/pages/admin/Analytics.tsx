import { useAdminAnalytics } from "../../hooks/useApiData";
import { Card, SectionHeading, StatCard } from "../../components/ui/Primitives";
import { RegistrationsChart } from "../../components/charts/RegistrationsChart";
import {
  AdminAttemptActivityChart,
  AdminCertificationPerformanceChart,
  AdminFailedTopicsChart,
  AdminRevenueChart,
} from "../../components/charts/AdminAnalyticsCharts";

export default function AdminAnalytics() {
  const { data, isLoading, isError } = useAdminAnalytics();

  if (isLoading) return <p>Loading analytics...</p>;
  if (isError || !data) return <p className="text-state-error">Could not load analytics.</p>;

  return (
    <div className="space-y-6">
      <SectionHeading title="Analytics" description="Live performance calculated from database records" />

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label="Total learners" value={data.totalLearners.toLocaleString()} />
        <StatCard label="Active learners (30d)" value={data.activeLearners.toLocaleString()} />
        <StatCard label="Questions answered" value={data.questionsAnswered.toLocaleString()} />
        <StatCard label="Question accuracy" value={`${data.questionAccuracyPercent}%`} />
        <StatCard label="Average score" value={`${data.averageScorePercent}%`} />
        <StatCard label="Pass rate" value={`${data.passRatePercent}%`} />
        <StatCard label="Mock exams completed" value={data.completedMockExams.toLocaleString()} />
        <StatCard label="Practice sessions completed" value={data.completedPracticeSessions.toLocaleString()} />
      </div>

      <Card className="p-5">
        <SectionHeading title="Learner registrations" description="New user registrations over the last 30 days" />
        <div className="h-72"><RegistrationsChart data={data.registrationsByDay} /></div>
      </Card>

      <Card className="p-5">
        <SectionHeading title="Practice vs mock exams" description="Completed sessions over the last 30 days" />
        <div className="h-72"><AdminAttemptActivityChart data={data.attemptsByDay} /></div>
      </Card>

      <Card className="p-5">
        <SectionHeading title="Certification performance" description="Completed attempts, pass rate and question accuracy" />
        <div className="h-80"><AdminCertificationPerformanceChart data={data.certificationPerformance} /></div>
      </Card>

      <div className="grid lg:grid-cols-2 gap-6">
        <Card className="p-5">
          <SectionHeading title="Most failed topics" description="Topics with the most incorrect answers from completed attempts" />
          <div className="h-80"><AdminFailedTopicsChart data={data.failedTopics} /></div>
        </Card>

        <Card className="p-5">
          <SectionHeading title="Revenue" description="Paid transactions by calendar month for the last 12 months" />
          <div className="h-80"><AdminRevenueChart data={data.revenueByMonth} /></div>
        </Card>
      </div>

      <Card className="p-5">
        <SectionHeading title="Analytics definition" description="All values on this page are calculated from persisted database records. No mock data is used by the admin analytics endpoint." />
        <p className="text-sm text-muted-foreground">
          Question accuracy is calculated from correct answers divided by answered questions. Pass rate is calculated from completed attempts that passed divided by all completed attempts. Revenue includes transactions whose status is Paid.
        </p>
      </Card>
    </div>
  );
}
