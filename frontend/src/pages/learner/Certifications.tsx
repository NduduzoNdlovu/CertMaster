import { Link } from "react-router-dom";
import { useCertifications } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function Certifications() {
  const { data, isLoading } = useCertifications();

  return (
    <div className="space-y-6">
      <SectionHeading title="Certifications" description="Choose a certification track to practice or take a mock exam" />

      {isLoading && <p className="text-sm text-text-secondary">Loading certifications...</p>}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {data?.map((cert) => (
          <Card key={cert.id} className="p-5 flex flex-col">
            <div className="flex items-start justify-between mb-2">
              <Badge tone="brand">{cert.code}</Badge>
              <span className="text-xs text-text-secondary">{cert.version}</span>
            </div>
            <h3 className="font-bold text-text-primary">{cert.name}</h3>
            <p className="text-sm text-text-secondary mt-1.5 flex-1">{cert.description}</p>
            <div className="flex items-center justify-between text-xs text-text-secondary mt-4 mb-4">
              <span>{cert.topicCount} topics</span>
              <span>{cert.questionCount}+ questions</span>
            </div>
            <div className="flex gap-2">
              <Link to={`/practice?cert=${cert.id}`} className="flex-1">
                <Button variant="secondary" fullWidth size="sm">Practice</Button>
              </Link>
              <Link to={`/mock-exams?cert=${cert.id}`} className="flex-1">
                <Button fullWidth size="sm">Mock exam</Button>
              </Link>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
