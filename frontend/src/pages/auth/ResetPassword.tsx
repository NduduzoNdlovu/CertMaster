import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "../../components/layout/AuthLayout";
import { Button } from "../../components/ui/Button";
import { api } from "../../lib/api";

const schema = z
  .object({
    token: z.string().min(6, "Enter the 6-digit code from your email"),
    newPassword: z.string().min(8, "Password must be at least 8 characters"),
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type FormValues = z.infer<typeof schema>;

export default function ResetPassword() {
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    setServerError(null);
    try {
      await api.post("/auth/reset-password", { token: values.token, newPassword: values.newPassword });
      navigate("/login");
    } catch {
      setServerError("That code is invalid or has expired. Request a new one and try again.");
    }
  };

  return (
    <AuthLayout title="Set a new password" subtitle="Enter the code we emailed you along with your new password">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && (
          <p className="text-sm text-state-error bg-red-50 border border-red-200 rounded-md px-3 py-2">{serverError}</p>
        )}

        <div>
          <label htmlFor="token" className="block text-sm font-medium text-text-primary mb-1.5">
            Reset code
          </label>
          <input
            id="token"
            inputMode="numeric"
            maxLength={6}
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm tracking-widest focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
            {...register("token")}
          />
          {errors.token && <p className="text-xs text-state-error mt-1">{errors.token.message}</p>}
        </div>

        <div>
          <label htmlFor="newPassword" className="block text-sm font-medium text-text-primary mb-1.5">
            New password
          </label>
          <input
            id="newPassword"
            type="password"
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
            {...register("newPassword")}
          />
          {errors.newPassword && <p className="text-xs text-state-error mt-1">{errors.newPassword.message}</p>}
        </div>

        <div>
          <label htmlFor="confirmPassword" className="block text-sm font-medium text-text-primary mb-1.5">
            Confirm new password
          </label>
          <input
            id="confirmPassword"
            type="password"
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
            {...register("confirmPassword")}
          />
          {errors.confirmPassword && <p className="text-xs text-state-error mt-1">{errors.confirmPassword.message}</p>}
        </div>

        <Button type="submit" fullWidth disabled={isSubmitting}>
          {isSubmitting ? "Resetting..." : "Reset password"}
        </Button>
      </form>

      <p className="text-center text-sm text-text-secondary mt-6">
        <Link to="/login" className="font-semibold text-brand-primary hover:underline">
          Back to sign in
        </Link>
      </p>
    </AuthLayout>
  );
}
