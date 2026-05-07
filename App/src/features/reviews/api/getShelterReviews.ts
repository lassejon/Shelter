import { apiClient } from '@/shared/api/client';
import type { SearchReviewByShelterResponse } from '@/features/reviews/models/dto';

interface GetShelterReviewsOptions {
  page?: number;
  pageSize?: number;
}

export async function getShelterReviews(
  shelterId: string,
  { page = 1, pageSize = 10 }: GetShelterReviewsOptions = {},
): Promise<SearchReviewByShelterResponse> {
  const { data } = await apiClient.get<SearchReviewByShelterResponse>(
    `/api/shelters/${shelterId}/reviews`,
    { params: { Page: page, PageSize: pageSize } },
  );
  return data;
}
