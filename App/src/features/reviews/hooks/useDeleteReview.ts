import { useMutation, useQueryClient } from '@tanstack/react-query';
import { deleteReview } from '@/features/reviews/api/deleteReview';

interface DeleteReviewArgs {
  reviewId: string;
  shelterId: string;
}

export function useDeleteReview() {
  const queryClient = useQueryClient();

  return useMutation<void, Error, DeleteReviewArgs>({
    mutationFn: ({ reviewId }) => deleteReview(reviewId),
    onSuccess: (_data, { shelterId }) => {
      queryClient.invalidateQueries({ queryKey: ['reviews', 'shelter', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['reviews', 'mine', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['reviews', 'pictures', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['shelters', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['shelters', 'bbox'] });
    },
  });
}
