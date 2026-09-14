import { Users, Crown, Activity, DollarSign } from "lucide-react";
import { useAdminOverview } from "../../hooks/useApiData";
import { Card, SectionHeading, StatCard, Badge } from "../../components/ui/Primitives";
import { RegistrationsChart } from "../../components/charts/RegistrationsChart";

export default function AdminDashboard() {
  const { data, isLoading, isError, refetch } = useAdminOverview();

  if (isLoading) return <p className="text-sm text-text-secondary">Loading overview...</p>;

  if (isError || !data) {
    return (
      <Card className="p-6">
        <SectionHeading title="Dashboard unavailable" description="The latest platform statistics could not be loaded." />
        <button
          type="button"
          onClick={() => refetch()}
          className="text-sm font-semibold text-brand-primary hover:underline"
        >
          Try again
        </button>
      </Card>
    );
  }

  const growthPrefix = data.monthlyGrowthPercent > 0 ? "+" : "";
  const statusTone = (status: string): "success" | "warning" =>
    status === "Online" || status === "Running" ? "success" : "warning";

  return (
    <div className="space-y-6">
      <SectionHeading title="Administrator overview" description="Platform-wide activity and health" />

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label="Total users" value={data.totalUsers.toLocaleString()} icon={<Users size={20} />} />
        <StatCard label="Premium users" value={data.premiumUsers.toLocaleString()} icon={<Crown size={20} />} />
        <StatCard label="Users online now" value={data.usersOnlineNow} icon={<Activity size={20} />} />
        <StatCard label="Monthly revenue" value={`R${data.monthlyRevenue.toLocaleString()}`} sublabel={`${growthPrefix}${data.monthlyGrowthPercent}% vs previous 30 days`} icon={<DollarSign size={20} />} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2 p-5">
          <SectionHeading title="Registrations" description="New sign-ups over the last 7 days" />
          <div className="h-64">
            <RegistrationsChart data={data.registrationsByDay} />
          </div>
        </Card>

        <Card className="p-5">
          <SectionHeading title="Live activity" />
          <div className="space-y-4 text-sm">
            <div className="flex justify-between"><span className="text-text-secondary">Active users today</span><span className="font-semibold text-text-primary">{data.activeUsersToday.toLocaleString()}</span></div>
            <div className="flex justify-between"><span className="text-text-secondary">Taking exams now</span><span className="font-semibold text-text-primary">{data.usersTakingExamsNow}</span></div>
            <div className="flex justify-between"><span className="text-text-secondary">Practice sessions running</span><span className="font-semibold text-text-primary">{data.practiceSessionsRunning}</span></div>
            <div className="flex justify-between"><span className="text-text-secondary">Most popular certification</span><span className="font-semibold text-text-primary">{data.mostPopularCertification}</span></div>
          </div>
        </Card>
      </div>

      <Card className="p-5">
        <SectionHeading title="System status" />
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="flex items-center justify-between p-4 rounded-md bg-bg-alt">
            <span className="text-sm text-text-primary">Database</span>
            <Badge tone={statusTone(data.systemStatus.database)}>{data.systemStatus.database}</Badge>
          </div>
          <div className="flex items-center justify-between p-4 rounded-md bg-bg-alt">
            <span className="text-sm text-text-primary">API</span>
            <Badge tone={statusTone(data.systemStatus.api)}>{data.systemStatus.api}</Badge>
          </div>
          <div className="flex items-center justify-between p-4 rounded-md bg-bg-alt">
            <span className="text-sm text-text-primary">Background jobs</span>
            <Badge tone={statusTone(data.systemStatus.backgroundJobs)}>{data.systemStatus.backgroundJobs}</Badge>
          </div>
        </div>
      </Card>
    </div>
  );
}
