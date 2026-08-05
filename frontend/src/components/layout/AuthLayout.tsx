import type { ReactNode } from "react";
import { GraduationCap } from "lucide-react";

export function AuthLayout({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  return (
    <div className="min-h-screen bg-bg-base flex items-center justify-center px-4 py-10">
      <div className="w-full max-w-md">
        <div className="flex items-center justify-center gap-2 mb-8">
          <div className="h-10 w-10 rounded-md bg-brand-primary flex items-center justify-center">
            <GraduationCap size={22} className="text-white" />
          </div>
          <span className="font-extrabold text-xl text-text-primary tracking-tight">CertMaster</span>
        </div>

        <div className="bg-bg-card border border-border-subtle rounded-lg shadow-sm p-6 sm:p-8">
          <h1 className="text-xl font-bold text-text-primary">{title}</h1>
          {subtitle && <p className="text-sm text-text-secondary mt-1 mb-6">{subtitle}</p>}
          {!subtitle && <div className="mb-6" />}
          {children}
        </div>

        <p className="text-center text-xs text-text-secondary mt-6">
          Learn. Practice. Pass.
        </p>
      </div>
    </div>
  );
}
