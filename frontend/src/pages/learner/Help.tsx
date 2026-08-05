import { Card, SectionHeading } from "../../components/ui/Primitives";

const faqs = [
  { q: "How is my exam readiness score calculated?", a: "It combines your recent mock exam scores, topic mastery, and consistency of practice over the last 30 days." },
  { q: "Can I retake a mock exam?", a: "Yes. Free accounts get a limited number of mock exams per month; Premium accounts get unlimited attempts." },
  { q: "How do I report an incorrect question?", a: "Use the flag icon on any practice question. Our content team reviews every report within 48 hours." },
  { q: "Can I use CertMaster on mobile?", a: "Yes, CertMaster is fully responsive and works on phones, tablets, and desktops." },
];

export default function Help() {
  return (
    <div className="space-y-6 max-w-3xl">
      <SectionHeading title="Help & support" description="Answers to common questions" />
      <Card className="divide-y divide-border-subtle">
        {faqs.map((f) => (
          <div key={f.q} className="p-5">
            <h3 className="font-semibold text-text-primary">{f.q}</h3>
            <p className="text-sm text-text-secondary mt-1.5">{f.a}</p>
          </div>
        ))}
      </Card>
    </div>
  );
}
