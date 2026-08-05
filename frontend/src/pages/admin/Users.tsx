import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

const users = [
  { id: 1, name: "Naledi Mokoena", email: "naledi@example.com", role: "Learner", plan: "Premium", status: "Active" },
  { id: 2, name: "Sipho Khumalo", email: "sipho@example.com", role: "Learner", plan: "Free", status: "Active" },
  { id: 3, name: "Amara Okafor", email: "amara@example.com", role: "Administrator", plan: "Premium", status: "Active" },
  { id: 4, name: "Liam Pretorius", email: "liam@example.com", role: "Learner", plan: "Free", status: "Suspended" },
];

export default function AdminUsers() {
  return (
    <div className="space-y-6">
      <SectionHeading
        title="Users"
        description="Search, filter, and manage learner and administrator accounts"
        action={<Button size="sm">Invite administrator</Button>}
      />

      <Card className="p-4">
        <input
          placeholder="Search by name or email..."
          className="w-full max-w-md h-11 px-3 rounded-md border border-border-subtle text-sm"
        />
      </Card>

      <Card className="p-0 overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-text-secondary bg-bg-alt">
              <th className="py-3 px-5 font-medium">Name</th>
              <th className="py-3 px-5 font-medium">Email</th>
              <th className="py-3 px-5 font-medium">Role</th>
              <th className="py-3 px-5 font-medium">Plan</th>
              <th className="py-3 px-5 font-medium">Status</th>
              <th className="py-3 px-5 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className="border-t border-border-subtle">
                <td className="py-3 px-5 text-text-primary font-medium">{u.name}</td>
                <td className="py-3 px-5 text-text-secondary">{u.email}</td>
                <td className="py-3 px-5"><Badge tone={u.role === "Administrator" ? "brand" : "neutral"}>{u.role}</Badge></td>
                <td className="py-3 px-5"><Badge tone={u.plan === "Premium" ? "info" : "neutral"}>{u.plan}</Badge></td>
                <td className="py-3 px-5"><Badge tone={u.status === "Active" ? "success" : "error"}>{u.status}</Badge></td>
                <td className="py-3 px-5">
                  <button className="text-brand-primary text-sm font-medium hover:underline">Manage</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
