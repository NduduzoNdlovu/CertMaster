import { useState } from "react";
import { Link } from "react-router-dom";
import { useAdminCertifications, useAdminResults } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function AdminResults() {
  const [search, setSearch] = useState(""); const [certificationId, setCertificationId] = useState(""); const [mode, setMode] = useState(""); const [passed, setPassed] = useState(""); const [page, setPage] = useState(1);
  const { data: certifications } = useAdminCertifications();
  const { data, isLoading } = useAdminResults({ search, certificationId, mode, passed, page, pageSize: 25 });
  return <div className="space-y-6">
    <SectionHeading title="Learner results" description="Review completed practice sessions and mock exams." />
    <Card className="p-4 grid gap-3 md:grid-cols-4">
      <input value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} placeholder="Learner name or email" className="h-11 px-3 rounded-md border" />
      <select value={certificationId} onChange={e => { setCertificationId(e.target.value); setPage(1); }} className="h-11 px-3 rounded-md border"><option value="">All certifications</option>{certifications?.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select>
      <select value={mode} onChange={e => { setMode(e.target.value); setPage(1); }} className="h-11 px-3 rounded-md border"><option value="">All modes</option><option value="Practice">Practice</option><option value="MockExam">Mock exam</option></select>
      <select value={passed} onChange={e => { setPassed(e.target.value); setPage(1); }} className="h-11 px-3 rounded-md border"><option value="">All outcomes</option><option value="true">Passed</option><option value="false">Failed</option></select>
    </Card>
    <Card className="p-0 overflow-x-auto">{isLoading ? <p className="p-5">Loading results...</p> : <>
      <table className="w-full text-sm"><thead><tr className="text-left bg-bg-alt"><th className="p-3">Learner</th><th>Certification</th><th>Mode</th><th>Score</th><th>Outcome</th><th>Completed</th><th></th></tr></thead><tbody>
      {data?.items.map(r => <tr key={r.id} className="border-t"><td className="p-3"><div className="font-medium">{r.userName}</div><div className="text-xs text-text-secondary">{r.userEmail}</div></td><td>{r.certificationName}</td><td>{r.mode}</td><td className="font-semibold">{r.score}%</td><td><Badge tone={r.passed ? "success" : "error"}>{r.passed ? "Passed" : "Failed"}</Badge></td><td>{new Date(r.completedAtUtc).toLocaleString()}</td><td><Link className="text-brand-primary font-medium hover:underline" to={`/admin/results/${r.id}`}>View</Link></td></tr>)}
      {data?.items.length === 0 && <tr><td colSpan={7} className="p-8 text-center text-text-secondary">No completed attempts found.</td></tr>}
      </tbody></table>
      <div className="p-4 flex items-center justify-between"><span>{data?.totalCount ?? 0} results</span><div className="flex gap-2"><Button size="sm" variant="secondary" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</Button><span className="text-sm self-center">Page {page} of {data?.totalPages || 1}</span><Button size="sm" variant="secondary" disabled={page >= (data?.totalPages || 1)} onClick={() => setPage(p => p + 1)}>Next</Button></div></div>
    </>}</Card>
  </div>;
}
