import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, PointElement, LineElement, Tooltip, Legend, Filler } from "chart.js";
import { Bar, Line } from "react-chartjs-2";

ChartJS.register(CategoryScale, LinearScale, BarElement, PointElement, LineElement, Tooltip, Legend, Filler);

const shortDate = (value: string) => {
  const date = new Date(`${value}T00:00:00Z`);
  return `${date.getUTCDate()}/${date.getUTCMonth() + 1}`;
};

export function AdminAttemptActivityChart({ data }: { data: { date: string; practice: number; mockExam: number }[] }) {
  return (
    <Line
      data={{
        labels: data.map((item) => shortDate(item.date)),
        datasets: [
          { label: "Practice", data: data.map((item) => item.practice), tension: 0.3 },
          { label: "Mock exams", data: data.map((item) => item.mockExam), tension: 0.3 },
        ],
      }}
      options={{ responsive: true, maintainAspectRatio: false, plugins: { legend: { position: "bottom" } }, scales: { y: { beginAtZero: true } } }}
    />
  );
}

export function AdminCertificationPerformanceChart({ data }: { data: { certification: string; passRatePercent: number; accuracyPercent: number }[] }) {
  return (
    <Bar
      data={{
        labels: data.map((item) => item.certification),
        datasets: [
          { label: "Pass rate %", data: data.map((item) => item.passRatePercent) },
          { label: "Question accuracy %", data: data.map((item) => item.accuracyPercent) },
        ],
      }}
      options={{ responsive: true, maintainAspectRatio: false, plugins: { legend: { position: "bottom" } }, scales: { y: { beginAtZero: true, max: 100 } } }}
    />
  );
}

export function AdminFailedTopicsChart({ data }: { data: { topic: string; incorrectAnswers: number }[] }) {
  return (
    <Bar
      data={{
        labels: data.map((item) => item.topic),
        datasets: [{ label: "Incorrect answers", data: data.map((item) => item.incorrectAnswers) }],
      }}
      options={{
        indexAxis: "y",
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: { x: { beginAtZero: true } },
      }}
    />
  );
}

export function AdminRevenueChart({ data }: { data: { month: string; revenue: number }[] }) {
  return (
    <Bar
      data={{
        labels: data.map((item) => item.month),
        datasets: [{ label: "Revenue (ZAR)", data: data.map((item) => item.revenue) }],
      }}
      options={{ responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } }}
    />
  );
}
