import { apiClient } from '@/shared/api/client';

export async function deleteReview(reviewId: string): Promise<void> {
  await apiClient.delete(`/api/reviews/${reviewId}`);
}
