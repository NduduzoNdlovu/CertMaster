import { Flag, RotateCcw, ShieldOff } from "lucide-react";
import { useOpenQuestionReports, useResolveQuestionReport } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";

export default function Reports() {
  const { data: reports, isLoading } = useOpenQuestionReports();
  const resolveReport = useResolveQuestionReport();

  return (
    <div className="space-y-6">
      <SectionHeading
        title="Reported questions"
        description="Questions learners have flagged as incorrect or unclear. Reporting automatically pulls a question out of practice and mock exams until it's resolved."
      />

      {isLoading && <p className="text-sm text-text-secondary">Loading reports...</p>}

      {!isLoading && reports?.length === 0 && (
        <Card className="p-8 text-center">
          <Flag size={28} className="text-text-secondary mx-auto mb-3" />
          <p className="text-sm text-text-secondary">No open reports — the question bank is all clear.</p>
        </Card>
      )}

      <div className="space-y-4">
        {reports?.map((r) => (
          <Card key={r.id} className="p-5">
            <div className="flex items-start justify-between gap-4 mb-3">
              <div>
                <Badge tone="warning">{r.questionStatus}</Badge>
                <p className="text-sm font-medium text-text-primary mt-2">{r.questionPrompt}</p>
              </div>
              <span className="text-xs text-text-secondary whitespace-nowrap">
                {new Date(r.createdAtUtc).toLocaleDateString()}
              </span>
            </div>

            <div className="bg-bg-alt rounded-md p-3 mb-4">
              <p className="text-xs text-text-secondary mb-1">Reported by {r.reportedByEmail}</p>
              <p className="text-sm text-text-primary">{r.reason}</p>
            </div>

            <div className="flex flex-wrap gap-2">
              <Button
                size="sm"
                onClick={() => resolveReport.mutate({ id: r.id, action: "Republish" })}
                disabled={resolveReport.isPending}
              >
                <RotateCcw size={14} /> Republish (false alarm / fixed)
              </Button>
              <Button
                size="sm"
                variant="secondary"
                onClick={() => resolveReport.mutate({ id: r.id, action: "KeepFlagged" })}
                disabled={resolveReport.isPending}
              >
                <ShieldOff size={14} /> Keep flagged (needs editing)
              </Button>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
