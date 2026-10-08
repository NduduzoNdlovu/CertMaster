import { Link, useParams } from "react-router-dom";
import { useAdminUser } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";

export default function UserDetails() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError } = useAdminUser(id);
  if (isLoading) return <p className="text-sm text-text-secondary">Loading user...</p>;
  if (isError || !data) return <Card className="p-6"><p className="text-state-error">User could not be loaded.</p></Card>;
  return <div className="space-y-6">
    <SectionHeading title={data.fullName} description={data.email} action={<Link to="/admin/users" className="text-sm font-semibold text-brand-primary hover:underline">Back to users</Link>} />
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      <Card className="p-5"><p className="text-sm text-text-secondary">Role</p><p className="font-semibold mt-1">{data.role}</p></Card>
      <Card className="p-5"><p className="text-sm text-text-secondary">Plan</p><p className="font-semibold mt-1">{data.plan}</p></Card>
      <Card className="p-5"><p className="text-sm text-text-secondary">Status</p><div className="mt-1"><Badge tone={data.isSuspended ? "error" : "success"}>{data.isSuspended ? "Suspended" : "Active"}</Badge></div></Card>
      <Card className="p-5"><p className="text-sm text-text-secondary">Completed attempts</p><p className="text-2xl font-bold mt-1">{data.completedAttempts}</p></Card>
      <Card className="p-5"><p className="text-sm text-text-secondary">Average score</p><p className="text-2xl font-bold mt-1">{data.averageScore.toFixed(1)}%</p></Card>
      <Card className="p-5"><p className="text-sm text-text-secondary">Study streak</p><p className="text-2xl font-bold mt-1">{data.studyStreakDays} days</p></Card>
    </div>
    <Card className="p-5 text-sm space-y-2">
      <p><strong>Registered:</strong> {new Date(data.createdAtUtc).toLocaleString()}</p>
      <p><strong>Email confirmed:</strong> {data.emailConfirmed ? "Yes" : "No"}</p>
      <p><strong>Last activity:</strong> {data.lastActivityAtUtc ? new Date(data.lastActivityAtUtc).toLocaleString() : "Never"}</p>
      <p><strong>Premium expiry:</strong> {data.premiumExpiresAtUtc ? new Date(data.premiumExpiresAtUtc).toLocaleString() : "—"}</p>
    </Card>
  </div>;
}
