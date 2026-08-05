import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Tooltip } from "chart.js";
import { Bar } from "react-chartjs-2";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);

export function RegistrationsChart({ data = [] }: { data?: { date: string; count: number }[] }) {
  return (
    <Bar
      data={{
        labels: data.map((d) => d.date),
        datasets: [
          {
            label: "New registrations",
            data: data.map((d) => d.count),
            backgroundColor: "#C62828",
            borderRadius: 4,
            maxBarThickness: 32,
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
