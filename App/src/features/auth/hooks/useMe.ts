import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fetchMe } from '@/features/auth/api/me';
import { useAuthStore } from '@/features/auth/stores/auth.store';

/**
 * Originally intended as ad-hoc session renewal (periodic /me refetches → fresh JWT) to defer
 * implementing real refresh tokens. With the current cache config (no refetchInterval,
 * refetchOnWindowFocus: false globally), this only fires once at app boot. Effective role is
 * boot-time identity reconciliation (stale localStorage vs. fresh roles/profile, account-deleted
 * detection). Session extension belongs to refresh tokens — TODO.
 */
export function useMe() {
  const token = useAuthStore((state) => state.token);
  const setAuth = useAuthStore((state) => state.setAuth);

  const query = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: fetchMe,
    enabled: Boolean(token),
    staleTime: 0.5 * 60 * 1000, // 5 minutes
    retry: false,
  });

  useEffect(() => {
    if (query.data) {
      setAuth(query.data);
    }
  }, [query.data, setAuth]);

  return query;
}
