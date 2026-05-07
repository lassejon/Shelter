import { AxiosError } from 'axios';
import { apiClient } from '@/shared/api/client';
import type { ReviewDetailResponse } from '@/features/reviews/models/dto';

/** Returns null when the current user has not reviewed this shelter (API responds 404). */
export async function getMyReview(shelterId: string): Promise<ReviewDetailResponse | null> {
  try {
    const { data } = await apiClient.get<ReviewDetailResponse>(
      `/api/shelters/${shelterId}/reviews/mine`,
    );
    return data;
  } catch (error) {
    if (error instanceof AxiosError && error.response?.status === 404) {
      return null;
    }
    throw error;
  }
}
