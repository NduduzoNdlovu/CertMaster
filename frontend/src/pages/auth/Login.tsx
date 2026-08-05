import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "../../components/layout/AuthLayout";
import { Button } from "../../components/ui/Button";
import { useAuth } from "../../context/AuthContext";

const schema = z.object({
  email: z.string().email("Enter a valid email address"),
  password: z.string().min(8, "Password must be at least 8 characters"),
  remember: z.boolean().optional(),
});

type FormValues = z.infer<typeof schema>;

export default function Login() {
  const { login } = useAuth();
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
      await login(values.email, values.password);
      navigate("/dashboard");
    } catch {
      setServerError("Incorrect email or password. Please try again.");
    }
  };

  return (
    <AuthLayout title="Welcome back" subtitle="Sign in to continue your exam preparation">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && (
          <p className="text-sm text-state-error bg-red-50 border border-red-200 rounded-md px-3 py-2">{serverError}</p>
        )}

        <div>
          <label htmlFor="email" className="block text-sm font-medium text-text-primary mb-1.5">
            Email address
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
            {...register("email")}
          />
          {errors.email && <p className="text-xs text-state-error mt-1">{errors.email.message}</p>}
        </div>

        <div>
          <label htmlFor="password" className="block text-sm font-medium text-text-primary mb-1.5">
            Password
          </label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
            {...register("password")}
          />
          {errors.password && <p className="text-xs text-state-error mt-1">{errors.password.message}</p>}
        </div>

        <div className="flex items-center justify-between">
          <label className="flex items-center gap-2 text-sm text-text-secondary">
            <input type="checkbox" className="h-4 w-4 rounded border-border-subtle" {...register("remember")} />
            Remember me
          </label>
          <Link to="/forgot-password" className="text-sm font-medium text-brand-primary hover:underline">
            Forgot password?
          </Link>
        </div>

        <Button type="submit" fullWidth disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </Button>
      </form>

      <p className="text-center text-sm text-text-secondary mt-6">
        Don't have an account?{" "}
        <Link to="/register" className="font-semibold text-brand-primary hover:underline">
          Create one
        </Link>
      </p>
    </AuthLayout>
  );
}
