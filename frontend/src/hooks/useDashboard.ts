import { useQuery } from "@tanstack/react-query";
import { api } from "../lib/api";
import { mockDashboard } from "../lib/mockData";
import type { DashboardSummary } from "../types";

const USE_MOCKS = import.meta.env.DEV && import.meta.env.VITE_USE_MOCKS === "true";

export function useDashboard() {
  return useQuery<DashboardSummary>({
    queryKey: ["dashboard", "summary"],
    queryFn: async () => {
      if (USE_MOCKS) return mockDashboard;
      const { data } = await api.get<DashboardSummary>("/dashboard/summary");
      return data;
    },
  });
}
