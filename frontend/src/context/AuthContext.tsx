import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api, tokenStorage } from "../lib/api";
import type { AuthUser } from "../types";

const USE_MOCKS = import.meta.env.VITE_USE_MOCKS === "true";

interface AuthContextValue {
  user: AuthUser | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (fullName: string, email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const MOCK_USER: AuthUser = {
  id: "user-1",
  fullName: "Thabo Nkosi",
  email: "thabo@example.com",
  role: "Learner",
  isPremium: false,
};

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function bootstrap() {
      const token = tokenStorage.getAccessToken();
      if (!token) {
        setIsLoading(false);
        return;
      }
      if (USE_MOCKS) {
        setUser(MOCK_USER);
        setIsLoading(false);
        return;
      }
      try {
        const { data } = await api.get<AuthUser>("/auth/me");
        setUser(data);
      } catch {
        tokenStorage.clear();
      } finally {
        setIsLoading(false);
      }
    }
    bootstrap();
  }, []);

  const login = async (email: string, password: string) => {
    if (USE_MOCKS) {
      tokenStorage.setTokens("mock-access-token", "mock-refresh-token");
      setUser({ ...MOCK_USER, email });
      return;
    }
    const { data } = await api.post("/auth/login", { email, password });
    tokenStorage.setTokens(data.accessToken, data.refreshToken);
    setUser(data.user);
  };

  const register = async (fullName: string, email: string, password: string) => {
    if (USE_MOCKS) {
      tokenStorage.setTokens("mock-access-token", "mock-refresh-token");
      setUser({ ...MOCK_USER, fullName, email });
      return;
    }
    const { data } = await api.post("/auth/register", { fullName, email, password });
    tokenStorage.setTokens(data.accessToken, data.refreshToken);
    setUser(data.user);
  };

  const logout = () => {
    tokenStorage.clear();
    setUser(null);
  };

  const value = useMemo(
    () => ({ user, isLoading, login, register, logout }),
    [user, isLoading]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
