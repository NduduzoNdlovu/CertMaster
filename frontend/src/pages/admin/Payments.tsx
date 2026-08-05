import { Card, SectionHeading, Badge, StatCard } from "../../components/ui/Primitives";

const transactions = [
  { id: "txn-1001", user: "Naledi Mokoena", plan: "Yearly", amount: 450, date: "2026-07-28", status: "Paid" },
  { id: "txn-1002", user: "Sipho Khumalo", plan: "Monthly", amount: 40, date: "2026-07-27", status: "Paid" },
  { id: "txn-1003", user: "Zanele Dube", plan: "Monthly", amount: 40, date: "2026-07-25", status: "Refunded" },
  { id: "txn-1004", user: "Liam Pretorius", plan: "Yearly", amount: 450, date: "2026-07-20", status: "Failed" },
];

export default function Payments() {
  return (
    <div className="space-y-6">
      <SectionHeading title="Payments & subscriptions" description="Revenue, billing, and subscription management" />

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label="Monthly revenue" value="R148,500" sublabel="+8.4% MoM" />
        <StatCard label="Active subscriptions" value="3,120" />
        <StatCard label="Monthly plans" value="2,410" />
        <StatCard label="Yearly plans" value="710" />
      </div>

      <Card className="p-0 overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-text-secondary bg-bg-alt">
              <th className="py-3 px-5 font-medium">Transaction</th>
              <th className="py-3 px-5 font-medium">User</th>
              <th className="py-3 px-5 font-medium">Plan</th>
              <th className="py-3 px-5 font-medium">Amount</th>
              <th className="py-3 px-5 font-medium">Date</th>
              <th className="py-3 px-5 font-medium">Status</th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((t) => (
              <tr key={t.id} className="border-t border-border-subtle">
                <td className="py-3 px-5 text-text-secondary font-mono text-xs">{t.id}</td>
                <td className="py-3 px-5 text-text-primary font-medium">{t.user}</td>
                <td className="py-3 px-5 text-text-secondary">{t.plan}</td>
                <td className="py-3 px-5 text-text-primary">R{t.amount}</td>
                <td className="py-3 px-5 text-text-secondary">{t.date}</td>
                <td className="py-3 px-5">
                  <Badge tone={t.status === "Paid" ? "success" : t.status === "Refunded" ? "warning" : "error"}>{t.status}</Badge>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
