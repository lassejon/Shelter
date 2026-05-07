import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateReview } from '@/features/reviews/api/updateReview';
import type {
  CreateReviewInput,
} from '@/features/reviews/models/createReview.schema';
import type { ReviewDetailResponse } from '@/features/reviews/models/dto';

interface UpdateReviewArgs {
  reviewId: string;
  shelterId: string;
  rating?: CreateReviewInput['rating'];
  comment?: string;
  newPictures?: File[];
  pictureIdsToDelete?: string[];
}

export function useUpdateReview() {
  const queryClient = useQueryClient();

  return useMutation<ReviewDetailResponse, Error, UpdateReviewArgs>({
    mutationFn: ({ reviewId, ...rest }) =>
      updateReview(reviewId, {
        rating: rest.rating,
        comment: rest.comment,
        newPictures: rest.newPictures,
        pictureIdsToDelete: rest.pictureIdsToDelete,
      }),
    onSuccess: (_data, { shelterId }) => {
      queryClient.invalidateQueries({ queryKey: ['reviews', 'shelter', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['reviews', 'mine', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['reviews', 'pictures', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['shelters', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['shelters', 'bbox'] });
    },
  });
}
