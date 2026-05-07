import type { components } from '@/shared/api/types/paths';

export type ReviewDetailResponse = components['schemas']['ReviewDetailResponse'];
export type SearchReviewByShelterResponse = components['schemas']['SearchReviewByShelterResponse'];
export type SearchReviewPictureResponse = components['schemas']['SearchReviewPictureResponse'];
export type CreateReviewRequest = components['schemas']['CreateReviewRequest'];
export type UpdateReviewRequest = components['schemas']['UpdateReviewRequest'];
export type Pagination = components['schemas']['Pagination'];
export type ReviewPictureResponse = components['schemas']['PictureResponse'];

/** API exposes Rating as `number`; UI constrains to 1..5. */
export type Rating = 1 | 2 | 3 | 4 | 5;

export const RATING_VALUES: Rating[] = [1, 2, 3, 4, 5];
