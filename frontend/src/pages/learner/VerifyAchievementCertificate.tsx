import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { api } from "../../lib/api";

type Verified = { certificateType: string; verificationCode: string; learnerName: string; certificationName: string; scorePercent: number; tier: string; issuedAtUtc: string; issuer: string };
export default function VerifyAchievementCertificate() {
 const { code = "" } = useParams(); const [item,setItem] = useState<Verified|null>(null); const [loading,setLoading] = useState(true);
 useEffect(()=>{ api.get<Verified>(`/achievement-certificates/verify/${encodeURIComponent(code)}`).then(r=>setItem(r.data)).catch(()=>setItem(null)).finally(()=>setLoading(false)); },[code]);
 return <main className="mx-auto max-w-2xl p-6 md:p-12"><div className="rounded-2xl border bg-white p-8 shadow-sm"><p className="text-sm font-semibold uppercase tracking-wide text-slate-500">CertMaster verification</p><h1 className="mt-2 text-2xl font-bold">Achievement Certificate</h1>{loading ? <p className="mt-6">Checking verification code…</p> : item ? <><p className="mt-5 rounded-lg bg-emerald-50 p-3 font-semibold text-emerald-800">Valid CertMaster achievement record</p><dl className="mt-6 space-y-4 text-sm">{[["Learner",item.learnerName],["Achievement",item.tier],["Certification track",item.certificationName],["Mock-exam score",`${item.scorePercent}%`],["Issued",new Date(item.issuedAtUtc).toLocaleDateString()],["Verification code",item.verificationCode],["Issuer",item.issuer]].map(([k,v])=><div key={k}><dt className="text-slate-500">{k}</dt><dd className="font-semibold">{v}</dd></div>)}</dl><p className="mt-6 text-xs text-slate-500">This confirms a CertMaster practice-platform achievement. It is not an official vendor certification.</p></> : <p role="alert" className="mt-6 rounded-lg bg-red-50 p-4 text-red-700">This certificate could not be verified. The code may be invalid.</p>}</div></main>;
}
