import { useAdminOverview } from "../../hooks/useApiData";
import { Card, SectionHeading, StatCard } from "../../components/ui/Primitives";
import { RegistrationsChart } from "../../components/charts/RegistrationsChart";

export default function AdminAnalytics() {
  const { data, isLoading } = useAdminOverview();
  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading analytics...</p>;

  return (
    <div className="space-y-6">
      <SectionHeading title="Analytics" description="Deep dive into platform performance" />

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label="Question accuracy" value="71%" sublabel="Across all certifications" />
        <StatCard label="Avg. exam duration" value="74m" sublabel="Mock exams" />
        <StatCard label="Most failed topic" value="Subnetting" />
        <StatCard label="Churn rate" value="3.2%" sublabel="Monthly" />
      </div>

      <Card className="p-5">
        <SectionHeading title="Registrations by day" />
        <div className="h-64">
          <RegistrationsChart data={data.registrationsByDay} />
        </div>
      </Card>
    </div>
  );
}
