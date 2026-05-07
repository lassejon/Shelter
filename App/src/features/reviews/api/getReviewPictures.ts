import { apiClient } from '@/shared/api/client';
import type { SearchReviewPictureResponse } from '@/features/reviews/models/dto';

interface GetReviewPicturesOptions {
  page?: number;
  pageSize?: number;
}

export async function getReviewPictures(
  shelterId: string,
  { page = 1, pageSize = 50 }: GetReviewPicturesOptions = {},
): Promise<SearchReviewPictureResponse> {
  const { data } = await apiClient.get<SearchReviewPictureResponse>(
    `/api/shelters/${shelterId}/reviews/pictures`,
    { params: { Page: page, PageSize: pageSize } },
  );
  return data;
}
