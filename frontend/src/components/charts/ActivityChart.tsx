import { Chart as ChartJS, CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Filler } from "chart.js";
import { Line } from "react-chartjs-2";

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Filler);

// export function ActivityChart({ data }: { data: { date: string; minutes: number }[] }) {
export function ActivityChart({ data = [] }: { data?: { date: string; minutes: number }[] }) {
  return (
    <Line
      data={{
        labels: data.map((d) => d.date),
        datasets: [
          {
            label: "Minutes studied",
            data: data.map((d) => d.minutes),
            borderColor: "#8B0000",
            backgroundColor: "rgba(139, 0, 0, 0.08)",
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointBackgroundColor: "#8B0000",
          },
        ],
      }}
      options={{
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: { grid: { display: false }, ticks: { color: "#6B6B6B" } },
          y: { grid: { color: "#EAEAEA" }, ticks: { color: "#6B6B6B" }, beginAtZero: true },
        },
      }}
    />
  );
}
