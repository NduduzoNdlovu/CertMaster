import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAdminQuestion, useUpdateAdminQuestion } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function QuestionDetails() {
  const {id}=useParams<{id:string}>(); const {data,isLoading}=useAdminQuestion(id); const update=useUpdateAdminQuestion(); const [draft,setDraft]=useState<any>(null);
  useEffect(()=>{if(data)setDraft({topic:data.topic,subtopic:data.subtopic??"",difficulty:data.difficulty,prompt:data.prompt,explanation:data.explanation,reference:data.reference??"",questionType:data.questionType,status:data.status,options:data.options.map(o=>({...o}))})},[data]);
  if(isLoading||!draft)return <p>Loading question...</p>;
  const setOption=(i:number,patch:any)=>setDraft((d:any)=>({...d,options:d.options.map((o:any,j:number)=>j===i?{...o,...patch}:d.questionType==="Choice"&&patch.isCorrect?{...o,isCorrect:false}:o)}));
  const save=()=>update.mutate({id:id!,data:draft});
  return <div className="space-y-6"><SectionHeading title="Edit question" description={`${data?.certificationName} · ${data?.topic}`} action={<Link to="/admin/questions" className="text-sm font-semibold text-brand-primary hover:underline">Back to questions</Link>} />
    <Card className="p-5 space-y-4"><div className="grid md:grid-cols-3 gap-3"><input value={draft.topic} onChange={e=>setDraft((d:any)=>({...d,topic:e.target.value}))} placeholder="Topic" className="h-10 px-3 border rounded-md"/><input value={draft.subtopic} onChange={e=>setDraft((d:any)=>({...d,subtopic:e.target.value}))} placeholder="Subtopic" className="h-10 px-3 border rounded-md"/><select value={draft.difficulty} onChange={e=>setDraft((d:any)=>({...d,difficulty:e.target.value}))} className="h-10 px-3 border rounded-md"><option>Easy</option><option>Medium</option><option>Hard</option></select></div>
      <div className="grid md:grid-cols-2 gap-3"><select value={draft.questionType} onChange={e=>setDraft((d:any)=>({...d,questionType:e.target.value}))} className="h-10 px-3 border rounded-md"><option>Choice</option><option>MultipleResponse</option><option>Simulation</option></select><select value={draft.status} onChange={e=>setDraft((d:any)=>({...d,status:e.target.value}))} className="h-10 px-3 border rounded-md"><option>Published</option><option>Draft</option><option>Flagged</option></select></div>
      <textarea rows={4} value={draft.prompt} onChange={e=>setDraft((d:any)=>({...d,prompt:e.target.value}))} className="w-full p-3 border rounded-md" placeholder="Question"/>
      <div className="space-y-2">{draft.options.map((o:any,i:number)=><div key={o.id||i} className="flex gap-2 items-center"><input type={draft.questionType==="MultipleResponse"?"checkbox":"radio"} checked={o.isCorrect} onChange={e=>setOption(i,{isCorrect:e.target.checked})}/><input value={o.text} onChange={e=>setOption(i,{text:e.target.value})} className="flex-1 h-10 px-3 border rounded-md"/></div>)}</div>
      <textarea rows={3} value={draft.explanation} onChange={e=>setDraft((d:any)=>({...d,explanation:e.target.value}))} className="w-full p-3 border rounded-md" placeholder="Explanation"/><input value={draft.reference} onChange={e=>setDraft((d:any)=>({...d,reference:e.target.value}))} placeholder="Reference" className="w-full h-10 px-3 border rounded-md"/>
      <div className="flex items-center gap-3"><Button onClick={save} disabled={update.isPending}>{update.isPending?"Saving...":"Save changes"}</Button><Badge tone={draft.status==="Published"?"success":draft.status==="Flagged"?"warning":"neutral"}>{draft.status}</Badge></div>
    </Card>
  </div>;
}
