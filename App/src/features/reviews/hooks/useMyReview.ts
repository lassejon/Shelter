import { useQuery } from '@tanstack/react-query';
import { getMyReview } from '@/features/reviews/api/getMyReview';
import { useAuthStore } from '@/features/auth/stores/auth.store';

export function useMyReview(shelterId: string | undefined) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  return useQuery({
    queryKey: ['reviews', 'mine', shelterId],
    queryFn: () => getMyReview(shelterId!),
    enabled: Boolean(shelterId) && isAuthenticated,
    staleTime: 60 * 1000,
  });
}
