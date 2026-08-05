import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Link } from "react-router-dom";
import { AuthLayout } from "../../components/layout/AuthLayout";
import { Button } from "../../components/ui/Button";
import { api } from "../../lib/api";

const schema = z.object({ email: z.string().email("Enter a valid email address") });
type FormValues = z.infer<typeof schema>;

export default function ForgotPassword() {
  const [sent, setSent] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = async (values: FormValues) => {
    try {
      await api.post("/auth/forgot-password", values);
    } catch {
      // Intentionally show the same success state to avoid email enumeration
    } finally {
      setSent(true);
    }
  };

  if (sent) {
    return (
      <AuthLayout title="Check your email">
        <p className="text-sm text-text-secondary">
          If an account exists for that address, we've sent a link to reset your password. It expires in 30 minutes.
        </p>
        <Link to="/reset-password" className="inline-block mt-4 text-sm font-semibold text-brand-primary hover:underline">
          I have my code — reset password
        </Link>
        <br />
        <Link to="/login" className="inline-block mt-2 text-sm font-semibold text-brand-primary hover:underline">
          Back to sign in
        </Link>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Reset your password" subtitle="Enter your email and we'll send you a reset link">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-text-primary mb-1.5">
            Email address
          </label>
          <input
            id="email"
            type="email"
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
            {...register("email")}
          />
          {errors.email && <p className="text-xs text-state-error mt-1">{errors.email.message}</p>}
        </div>
        <Button type="submit" fullWidth disabled={isSubmitting}>
          {isSubmitting ? "Sending..." : "Send reset link"}
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
