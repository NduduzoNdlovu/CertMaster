import { useState } from "react";
import { Check, Loader2 } from "lucide-react";
import { Card, SectionHeading } from "../../components/ui/Primitives";
import { Button } from "../../components/ui/Button";
import { api } from "../../lib/api";

const USE_MOCKS = import.meta.env.VITE_USE_MOCKS === "true";

const freeFeatures = ["Limited mock exams", "Limited daily practice", "Basic analytics", "Basic explanations", "Limited bookmarks"];
const premiumFeatures = [
  "Unlimited mock exams",
  "Unlimited practice",
  "Adaptive learning",
  "Detailed analytics",
  "Unlimited bookmarks",
  "Full explanations",
  "Weak topic recommendations",
  "Flashcards",
  "Exam readiness score",
  "Priority access to new certification versions",
  "No advertisements",
  "Priority support",
];

export default function Premium() {
  const [loadingPlan, setLoadingPlan] = useState<"PremiumMonthly" | "PremiumYearly" | null>(null);
  const [result, setResult] = useState<{ succeeded: boolean; message: string } | null>(null);

  const upgrade = async (plan: "PremiumMonthly" | "PremiumYearly") => {
    setLoadingPlan(plan);
    setResult(null);
    try {
      if (USE_MOCKS) {
        await new Promise((r) => setTimeout(r, 800));
        setResult({ succeeded: true, message: "Upgrade successful! (Mock mode — no real charge was made.)" });
        return;
      }
      const { data } = await api.post("/billing/upgrade", { plan });
      setResult({
        succeeded: data.succeeded,
        message: data.succeeded
          ? "Upgrade successful! Refresh the page to see your new plan reflected across the app."
          : data.failureReason ?? "Payment failed. Please try again.",
      });
    } catch {
      setResult({ succeeded: false, message: "Something went wrong processing your upgrade. Please try again." });
    } finally {
      setLoadingPlan(null);
    }
  };

  return (
    <div className="space-y-6">
      <SectionHeading title="Premium" description="Unlock the full CertMaster experience" />

      {result && (
        <div className={`p-4 rounded-md border text-sm ${result.succeeded ? "bg-green-50 border-green-200 text-state-success" : "bg-red-50 border-red-200 text-state-error"}`}>
          {result.message}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card className="p-6">
          <h3 className="font-bold text-text-primary">Free</h3>
          <p className="text-3xl font-extrabold text-text-primary mt-2">R0</p>
          <ul className="mt-5 space-y-2.5">
            {freeFeatures.map((f) => (
              <li key={f} className="flex items-center gap-2 text-sm text-text-secondary">
                <Check size={16} className="text-text-secondary" /> {f}
              </li>
            ))}
          </ul>
        </Card>

        <Card className="p-6 border-2 border-brand-primary relative">
          <span className="absolute -top-3 left-6 bg-brand-primary text-white text-xs font-semibold px-3 py-1 rounded-full">
            Most popular
          </span>
          <h3 className="font-bold text-text-primary">Premium</h3>
          <p className="text-3xl font-extrabold text-text-primary mt-2">
            R40<span className="text-sm font-medium text-text-secondary"> /month</span>
          </p>
          <p className="text-xs text-text-secondary mt-1">or R450/year (save R30)</p>
          <ul className="mt-5 space-y-2.5">
            {premiumFeatures.map((f) => (
              <li key={f} className="flex items-center gap-2 text-sm text-text-primary">
                <Check size={16} className="text-state-success" /> {f}
              </li>
            ))}
          </ul>
          <div className="flex flex-col sm:flex-row gap-2 mt-6">
            <Button fullWidth onClick={() => upgrade("PremiumMonthly")} disabled={loadingPlan !== null}>
              {loadingPlan === "PremiumMonthly" ? <Loader2 size={16} className="animate-spin" /> : "Upgrade monthly — R40"}
            </Button>
            <Button fullWidth variant="secondary" onClick={() => upgrade("PremiumYearly")} disabled={loadingPlan !== null}>
              {loadingPlan === "PremiumYearly" ? <Loader2 size={16} className="animate-spin" /> : "Upgrade yearly — R450"}
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}
