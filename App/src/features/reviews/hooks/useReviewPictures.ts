import { useQuery } from '@tanstack/react-query';
import { getReviewPictures } from '@/features/reviews/api/getReviewPictures';

interface UseReviewPicturesOptions {
  page?: number;
  pageSize?: number;
  enabled?: boolean;
}

export function useReviewPictures(
  shelterId: string | undefined,
  { page = 1, pageSize = 50, enabled = true }: UseReviewPicturesOptions = {},
) {
  return useQuery({
    queryKey: ['reviews', 'pictures', shelterId, { page, pageSize }],
    queryFn: () => getReviewPictures(shelterId!, { page, pageSize }),
    enabled: enabled && Boolean(shelterId),
    staleTime: 60 * 1000,
  });
}
