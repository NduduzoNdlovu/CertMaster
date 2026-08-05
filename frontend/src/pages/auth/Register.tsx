import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "../../components/layout/AuthLayout";
import { Button } from "../../components/ui/Button";
import { useAuth } from "../../context/AuthContext";

const schema = z
  .object({
    fullName: z.string().min(2, "Enter your full name"),
    email: z.string().email("Enter a valid email address"),
    password: z.string().min(8, "Password must be at least 8 characters"),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type FormValues = z.infer<typeof schema>;

export default function Register() {
  const { register: registerUser } = useAuth();
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
      await registerUser(values.fullName, values.email, values.password);
      navigate("/dashboard");
    } catch {
      setServerError("We couldn't create your account. That email may already be registered.");
    }
  };

  return (
    <AuthLayout title="Create your account" subtitle="Start practicing for CompTIA A+ and Network+ today">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && (
          <p className="text-sm text-state-error bg-red-50 border border-red-200 rounded-md px-3 py-2">{serverError}</p>
        )}

        <div>
          <label htmlFor="fullName" className="block text-sm font-medium text-text-primary mb-1.5">
            Full name
          </label>
          <input
            id="fullName"
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
            {...register("fullName")}
          />
          {errors.fullName && <p className="text-xs text-state-error mt-1">{errors.fullName.message}</p>}
        </div>

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

        <div>
          <label htmlFor="password" className="block text-sm font-medium text-text-primary mb-1.5">
            Password
          </label>
          <input
            id="password"
            type="password"
            className="w-full h-11 px-3 rounded-md border border-border-subtle bg-white text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/40"
            {...register("password")}
          />
          {errors.password && <p className="text-xs text-state-error mt-1">{errors.password.message}</p>}
        </div>

        <div>
          <label htmlFor="confirmPassword" className="block text-sm font-medium text-text-primary mb-1.5">
            Confirm password
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
          {isSubmitting ? "Creating account..." : "Create account"}
        </Button>
      </form>

      <p className="text-center text-sm text-text-secondary mt-6">
        Already have an account?{" "}
        <Link to="/login" className="font-semibold text-brand-primary hover:underline">
          Sign in
        </Link>
      </p>
    </AuthLayout>
  );
}
