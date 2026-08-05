import { useMaintenanceWindows } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function Maintenance() {
  const { data: windows, isLoading } = useMaintenanceWindows();

  return (
    <div className="space-y-6">
      <SectionHeading
        title="Maintenance"
        description="Schedule downtime windows; the system blocks exams that can't finish before maintenance starts"
        action={<Button size="sm">Schedule window</Button>}
      />

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="p-5">
          <p className="text-sm text-text-secondary">Active practice sessions</p>
          <p className="text-2xl font-bold text-text-primary mt-1">128</p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-text-secondary">Active mock exams</p>
          <p className="text-2xl font-bold text-text-primary mt-1">42</p>
        </Card>
        <Card className="p-5">
          <p className="text-sm text-text-secondary">Users online</p>
          <p className="text-2xl font-bold text-text-primary mt-1">314</p>
        </Card>
      </div>

      <Card className="p-5">
        <SectionHeading title="Scheduled windows" />
        {isLoading && <p className="text-sm text-text-secondary">Loading...</p>}
        <div className="space-y-3">
          {windows?.map((w) => (
            <div key={w.id} className="flex items-center justify-between p-4 rounded-md bg-bg-alt">
              <div>
                <p className="text-sm font-medium text-text-primary">
                  {new Date(w.startsAt).toLocaleString()} — {new Date(w.endsAt).toLocaleTimeString()}
                </p>
                <p className="text-xs text-text-secondary mt-1">{w.reason}</p>
              </div>
              <div className="flex items-center gap-3">
                <Badge tone={w.status === "Active" ? "warning" : "neutral"}>{w.status}</Badge>
                <button className="text-state-error text-sm font-medium hover:underline">Cancel</button>
              </div>
            </div>
          ))}
        </div>
      </Card>

      <Card className="p-5 border-l-4 border-l-brand-accent">
        <SectionHeading title="Emergency force maintenance" description="Immediately blocks new sessions and warns all active users. Every action is logged to the audit trail." />
        <Button variant="danger">Force maintenance now</Button>
      </Card>
    </div>
  );
}
