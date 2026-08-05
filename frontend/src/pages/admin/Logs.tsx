import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";

const logs = [
  { id: 1, actor: "admin@certmaster.com", action: "Uploaded new question bank version", target: "CompTIA Network+ N10-009", time: "2026-07-29 14:22", level: "Info" },
  { id: 2, actor: "admin@certmaster.com", action: "Scheduled maintenance window", target: "2026-08-02 02:00–02:30", time: "2026-07-28 09:10", level: "Info" },
  { id: 3, actor: "system", action: "Failed login attempts threshold reached", target: "user 'liam@example.com'", time: "2026-07-27 22:05", level: "Warning" },
  { id: 4, actor: "admin@certmaster.com", action: "Suspended user account", target: "liam@example.com", time: "2026-07-27 22:10", level: "Warning" },
  { id: 5, actor: "system", action: "Payment webhook processing error", target: "txn-1004", time: "2026-07-20 11:44", level: "Error" },
];

export default function Logs() {
  return (
    <div className="space-y-6">
      <SectionHeading title="Audit logs" description="Every administrative and system-critical event, in order" />
      <Card className="p-0 overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-text-secondary bg-bg-alt">
              <th className="py-3 px-5 font-medium">Time</th>
              <th className="py-3 px-5 font-medium">Actor</th>
              <th className="py-3 px-5 font-medium">Action</th>
              <th className="py-3 px-5 font-medium">Target</th>
              <th className="py-3 px-5 font-medium">Level</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((l) => (
              <tr key={l.id} className="border-t border-border-subtle">
                <td className="py-3 px-5 text-text-secondary whitespace-nowrap">{l.time}</td>
                <td className="py-3 px-5 text-text-secondary">{l.actor}</td>
                <td className="py-3 px-5 text-text-primary">{l.action}</td>
                <td className="py-3 px-5 text-text-secondary">{l.target}</td>
                <td className="py-3 px-5">
                  <Badge tone={l.level === "Error" ? "error" : l.level === "Warning" ? "warning" : "info"}>{l.level}</Badge>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
