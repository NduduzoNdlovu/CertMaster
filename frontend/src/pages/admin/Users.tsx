import { useState } from "react";
import { useAdminUsers, useSetUserSuspended } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function AdminUsers() {
  const [search, setSearch] = useState(""); const [role, setRole] = useState(""); const [plan, setPlan] = useState(""); const [status, setStatus] = useState(""); const [page, setPage] = useState(1);
  const { data, isLoading, isError } = useAdminUsers({ search, role, plan, status, page, pageSize: 25 });
  const setSuspended = useSetUserSuspended();
  return <div className="space-y-6"><SectionHeading title="Users" description="Search, filter, and manage database accounts" />
    <Card className="p-4 grid gap-3 md:grid-cols-4"><input value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} placeholder="Name or email" className="h-11 px-3 rounded-md border" />
      <select value={role} onChange={e => setRole(e.target.value)} className="h-11 px-3 rounded-md border"><option value="">All roles</option><option>Learner</option><option>Administrator</option></select>
      <select value={plan} onChange={e => setPlan(e.target.value)} className="h-11 px-3 rounded-md border"><option value="">All plans</option><option>Free</option><option>PremiumMonthly</option><option>PremiumYearly</option></select>
      <select value={status} onChange={e => setStatus(e.target.value)} className="h-11 px-3 rounded-md border"><option value="">All statuses</option><option>Active</option><option>Suspended</option></select></Card>
    {isLoading ? <p>Loading users...</p> : isError ? <p className="text-state-error">Could not load users.</p> : <Card className="p-0 overflow-x-auto"><table className="w-full text-sm"><thead><tr className="text-left bg-bg-alt text-text-secondary"><th className="py-3 px-5">Name</th><th>Email</th><th>Role</th><th>Plan</th><th>Status</th><th>Action</th></tr></thead><tbody>{data?.items.map(u => <tr key={u.id} className="border-t"><td className="py-3 px-5 font-medium">{u.fullName}</td><td>{u.email}</td><td><Badge tone={u.role === "Administrator" ? "brand" : "neutral"}>{u.role}</Badge></td><td>{u.plan}</td><td><Badge tone={u.isSuspended ? "error" : "success"}>{u.isSuspended ? "Suspended" : "Active"}</Badge></td><td><Button size="sm" variant={u.isSuspended ? "primary" : "danger"} disabled={setSuspended.isPending} onClick={() => setSuspended.mutate({ id: u.id, suspended: !u.isSuspended })}>{u.isSuspended ? "Reactivate" : "Suspend"}</Button></td></tr>)}</tbody></table>
      <div className="p-4 flex items-center justify-between"><span className="text-sm">{data?.totalCount ?? 0} users</span><div className="flex gap-2"><Button size="sm" variant="secondary" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</Button><span>Page {page} of {data?.totalPages || 1}</span><Button size="sm" variant="secondary" disabled={page >= (data?.totalPages || 1)} onClick={() => setPage(p => p + 1)}>Next</Button></div></div></Card>}
  </div>;
}
