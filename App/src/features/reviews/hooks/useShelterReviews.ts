import { useQuery } from '@tanstack/react-query';
import { getShelterReviews } from '@/features/reviews/api/getShelterReviews';

interface UseShelterReviewsOptions {
  page?: number;
  pageSize?: number;
  enabled?: boolean;
}

export function useShelterReviews(
  shelterId: string | undefined,
  { page = 1, pageSize = 10, enabled = true }: UseShelterReviewsOptions = {},
) {
  return useQuery({
    queryKey: ['reviews', 'shelter', shelterId, { page, pageSize }],
    queryFn: () => getShelterReviews(shelterId!, { page, pageSize }),
    enabled: enabled && Boolean(shelterId),
    staleTime: 30 * 1000,
    placeholderData: (previousData) => previousData,
  });
}
