import { useState } from "react";
import { useLeaderboard, useMyLeaderboardRank } from "../../hooks/useApiData";
import { Card, SectionHeading, Badge } from "../../components/ui/Primitives";

export default function Leaderboard() {
  const [period, setPeriod] = useState<"AllTime" | "Monthly">("AllTime");
  const { data: leaderboard, isLoading } = useLeaderboard(10, period);
  const { data: myRank } = useMyLeaderboardRank(period);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <SectionHeading title="Leaderboard" description="Top learners ranked by points earned from practice and mock exams" />
        <div className="flex rounded-md border border-border-subtle overflow-hidden w-fit">
          <button
            onClick={() => setPeriod("AllTime")}
            className={`h-9 px-4 text-sm font-medium ${period === "AllTime" ? "bg-brand-primary text-white" : "bg-white text-text-secondary hover:bg-bg-alt"}`}
          >
            All-time
          </button>
          <button
            onClick={() => setPeriod("Monthly")}
            className={`h-9 px-4 text-sm font-medium ${period === "Monthly" ? "bg-brand-primary text-white" : "bg-white text-text-secondary hover:bg-bg-alt"}`}
          >
            This month
          </button>
        </div>
      </div>

      {isLoading && <p className="text-sm text-text-secondary">Loading leaderboard...</p>}

      <Card className="p-0 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-text-secondary bg-bg-alt">
              <th className="py-3 px-5 font-medium">Rank</th>
              <th className="py-3 px-5 font-medium">Learner</th>
              <th className="py-3 px-5 font-medium">Points</th>
            </tr>
          </thead>
          <tbody>
            {leaderboard?.map((row) => (
              <tr key={row.rank} className="border-t border-border-subtle">
                <td className="py-3 px-5 font-semibold text-text-primary">#{row.rank}</td>
                <td className="py-3 px-5 text-text-primary flex items-center gap-2">
                  {row.fullName} {row.isPremium && <Badge tone="brand">Premium</Badge>}
                </td>
                <td className="py-3 px-5 text-text-secondary">{row.points.toLocaleString()}</td>
              </tr>
            ))}
            {leaderboard?.length === 0 && (
              <tr>
                <td colSpan={3} className="py-6 px-5 text-center text-text-secondary">
                  No completed exams {period === "Monthly" ? "this month" : "yet"} — be the first on the board!
                </td>
              </tr>
            )}
            {myRank && (
              <tr className="border-t border-border-subtle bg-bg-alt">
                <td className="py-3 px-5 font-semibold text-brand-primary">#{myRank.rank}</td>
                <td className="py-3 px-5 text-text-primary">{myRank.fullName} (you)</td>
                <td className="py-3 px-5 text-text-secondary">{myRank.points.toLocaleString()}</td>
              </tr>
            )}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
