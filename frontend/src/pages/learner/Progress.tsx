import { useDashboard } from "../../hooks/useDashboard";
import { Card, ProgressBar, SectionHeading } from "../../components/ui/Primitives";
import { ActivityChart } from "../../components/charts/ActivityChart";

export default function Progress() {
  const { data, isLoading } = useDashboard();
  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading progress...</p>;

  const allTopics = [...data.strongTopics, ...data.weakTopics].sort((a, b) => b.masteryPercent - a.masteryPercent);

  return (
    <div className="space-y-6">
      <SectionHeading title="Progress" description="Topic-by-topic mastery across your active certifications" />

      <Card className="p-5">
        <SectionHeading title="Study time trend" />
        <div className="h-56">
          <ActivityChart data={data.dailyActivity} />
        </div>
      </Card>

      <Card className="p-5">
        <SectionHeading title="Topic mastery" />
        <div className="space-y-4">
          {allTopics.map((t) => (
            <div key={t.topic}>
              <div className="flex justify-between text-sm mb-1.5">
                <span className="text-text-primary font-medium">{t.topic}</span>
                <span className="text-text-secondary">{t.masteryPercent}%</span>
              </div>
              <ProgressBar percent={t.masteryPercent} tone={t.masteryPercent >= 70 ? "success" : t.masteryPercent >= 50 ? "warning" : "error"} />
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}
