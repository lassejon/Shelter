import { apiClient } from '@/shared/api/client';
import type {
  CreateReviewInput,
} from '@/features/reviews/models/createReview.schema';
import type { ReviewDetailResponse } from '@/features/reviews/models/dto';

interface UpdateReviewArgs {
  rating?: CreateReviewInput['rating'];
  comment?: string;
  newPictures?: File[];
  pictureIdsToDelete?: string[];
}

export async function updateReview(
  reviewId: string,
  args: UpdateReviewArgs,
): Promise<ReviewDetailResponse> {
  const form = new FormData();
  if (args.rating !== undefined) form.append('rating', String(args.rating));
  if (args.comment !== undefined) form.append('comment', args.comment);
  args.newPictures?.forEach((file) => form.append('newPictures', file));
  args.pictureIdsToDelete?.forEach((id) => form.append('pictureIdsToDelete', id));

  const { data } = await apiClient.put<ReviewDetailResponse>(
    `/api/reviews/${reviewId}`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  );
  return data;
}
