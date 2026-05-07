import { apiClient } from '@/shared/api/client';
import type {
  CreateReviewInput,
} from '@/features/reviews/models/createReview.schema';
import type { ReviewDetailResponse } from '@/features/reviews/models/dto';

export async function createReview(
  shelterId: string,
  input: CreateReviewInput,
  pictures: File[],
): Promise<ReviewDetailResponse> {
  const form = new FormData();
  form.append('rating', String(input.rating));
  if (input.comment) form.append('comment', input.comment);
  pictures.forEach((file) => form.append('pictures', file));

  const { data } = await apiClient.post<ReviewDetailResponse>(
    `/api/shelters/${shelterId}/reviews`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  );
  return data;
}
