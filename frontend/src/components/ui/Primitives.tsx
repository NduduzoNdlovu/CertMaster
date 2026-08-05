import type { ReactNode } from "react";

export function Card({ children, className = "" }: { children: ReactNode; className?: string }) {
  return (
    <div className={`bg-bg-card border border-border-subtle rounded-lg shadow-sm ${className}`}>
      {children}
    </div>
  );
}

export function Badge({
  tone = "neutral",
  children,
}: {
  tone?: "neutral" | "success" | "warning" | "error" | "info" | "brand";
  children: ReactNode;
}) {
  const toneClasses: Record<string, string> = {
    neutral: "bg-bg-alt text-text-secondary border-border-subtle",
    success: "bg-green-50 text-state-success border-green-200",
    warning: "bg-orange-50 text-state-warning border-orange-200",
    error: "bg-red-50 text-state-error border-red-200",
    info: "bg-blue-50 text-state-info border-blue-200",
    brand: "bg-red-50 text-brand-primary border-red-200",
  };
  return (
    <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${toneClasses[tone]}`}>
      {children}
    </span>
  );
}

export function ProgressBar({ percent, tone = "brand" }: { percent: number; tone?: "brand" | "success" | "warning" | "error" }) {
  const toneClasses: Record<string, string> = {
    brand: "bg-brand-primary",
    success: "bg-state-success",
    warning: "bg-state-warning",
    error: "bg-state-error",
  };
  const clamped = Math.max(0, Math.min(100, percent));
  return (
    <div className="w-full h-2 bg-bg-alt rounded-full overflow-hidden" role="progressbar" aria-valuenow={clamped} aria-valuemin={0} aria-valuemax={100}>
      <div className={`h-full ${toneClasses[tone]} transition-all duration-300`} style={{ width: `${clamped}%` }} />
    </div>
  );
}

export function StatCard({
  label,
  value,
  sublabel,
  icon,
}: {
  label: string;
  value: string | number;
  sublabel?: string;
  icon?: ReactNode;
}) {
  return (
    <Card className="p-5 flex items-start justify-between">
      <div>
        <p className="text-sm text-text-secondary font-medium">{label}</p>
        <p className="text-2xl font-bold text-text-primary mt-1">{value}</p>
        {sublabel && <p className="text-xs text-text-secondary mt-1">{sublabel}</p>}
      </div>
      {icon && <div className="text-brand-primary bg-bg-alt p-2.5 rounded-md">{icon}</div>}
    </Card>
  );
}

export function SectionHeading({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="flex items-center justify-between flex-wrap gap-3 mb-4">
      <div>
        <h2 className="text-lg font-bold text-text-primary">{title}</h2>
        {description && <p className="text-sm text-text-secondary mt-0.5">{description}</p>}
      </div>
      {action}
    </div>
  );
}
