import { Navigate } from 'react-router';
import type { ReactNode } from 'react';
import { useAuthStore } from '@/features/auth/stores/auth.store';

interface RoleProtectedRouteProps {
  role: string;
  children: ReactNode;
}

export function RoleProtectedRoute({ role, children }: RoleProtectedRouteProps) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const roles = useAuthStore((state) => state.roles);

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  if (!roles.includes(role)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
