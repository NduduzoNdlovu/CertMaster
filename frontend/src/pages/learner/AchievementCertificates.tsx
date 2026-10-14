import { useEffect, useState } from "react";
import { Award, ExternalLink, LoaderCircle } from "lucide-react";
import { api } from "../../lib/api";

type Certificate = { id: string; certificationId: string; certificationName: string; examAttemptId: string; scorePercent: number; tier: string; verificationCode: string; issuedAtUtc: string };

export default function AchievementCertificates() {
  const [items, setItems] = useState<Certificate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  useEffect(() => {
    api.get<Certificate[]>("/achievement-certificates/mine").then(r => setItems(r.data)).catch(() => setError("We couldn't load your certificates. Please try again.")).finally(() => setLoading(false));
  }, []);
  const verifyUrl = (code: string) => `${window.location.origin}/verify-certificate/${encodeURIComponent(code)}`;
  return <section className="mx-auto max-w-5xl space-y-6 p-4 md:p-8">
    <div><h1 className="text-2xl font-bold text-slate-900">My Achievement Certificates</h1><p className="mt-1 text-sm text-slate-600">CertMaster recognition for mock-exam achievements. These are not official vendor certifications.</p></div>
    {loading ? <div className="flex items-center gap-2 py-10 text-slate-600"><LoaderCircle className="animate-spin" size={18}/> Loading certificates…</div> : error ? <p role="alert" className="rounded-lg bg-red-50 p-4 text-red-700">{error}</p> : items.length === 0 ? <div className="rounded-xl border border-dashed p-10 text-center"><Award className="mx-auto mb-3 text-slate-400" size={36}/><h2 className="font-semibold">No certificates yet</h2><p className="mt-2 text-sm text-slate-600">Complete a mock exam and score at least 75% to earn your first certificate.</p></div> : <div className="grid gap-4 md:grid-cols-2">{items.map(c => <article key={c.id} className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><div className="flex items-start justify-between gap-3"><div className="rounded-lg bg-amber-50 p-3 text-amber-700"><Award size={24}/></div><span className="rounded-full bg-emerald-50 px-3 py-1 text-sm font-semibold text-emerald-800">{c.tier}</span></div><h2 className="mt-4 text-lg font-bold">{c.certificationName}</h2><p className="mt-1 text-sm text-slate-600">CertMaster Achievement Certificate</p><div className="mt-4 flex items-baseline gap-2"><strong className="text-3xl">{c.scorePercent}%</strong><span className="text-sm text-slate-500">mock-exam score</span></div><p className="mt-3 text-xs text-slate-500">Issued {new Date(c.issuedAtUtc).toLocaleDateString()}</p><p className="mt-1 break-all font-mono text-xs text-slate-500">Verification: {c.verificationCode}</p><button onClick={() => window.open(verifyUrl(c.verificationCode), "_blank", "noopener,noreferrer")} className="mt-4 inline-flex items-center gap-2 rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white">Verify certificate <ExternalLink size={15}/></button></article>)}</div>}
  </section>;
}
