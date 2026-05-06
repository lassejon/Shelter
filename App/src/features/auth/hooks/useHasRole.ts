import { useAuthStore } from '@/features/auth/stores/auth.store';

export function useHasRole(role: string): boolean {
  return useAuthStore((state) => state.roles.includes(role));
}
