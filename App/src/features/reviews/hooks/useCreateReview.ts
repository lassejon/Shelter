import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createReview } from '@/features/reviews/api/createReview';
import type {
  CreateReviewInput,
} from '@/features/reviews/models/createReview.schema';
import type { ReviewDetailResponse } from '@/features/reviews/models/dto';

interface CreateReviewArgs {
  shelterId: string;
  input: CreateReviewInput;
  pictures: File[];
}

export function useCreateReview() {
  const queryClient = useQueryClient();

  return useMutation<ReviewDetailResponse, Error, CreateReviewArgs>({
    mutationFn: ({ shelterId, input, pictures }) =>
      createReview(shelterId, input, pictures),
    onSuccess: (_data, { shelterId }) => {
      queryClient.invalidateQueries({ queryKey: ['reviews', 'shelter', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['reviews', 'mine', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['reviews', 'pictures', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['shelters', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['shelters', 'bbox'] });
    },
  });
}
