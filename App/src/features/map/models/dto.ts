import type { components } from '@/shared/api/types/paths';

export type SearchShelterResponse = components['schemas']['SearchShelterResponse'];

export interface BoundingBox {
  minLatitude: number;
  maxLatitude: number;
  minLongitude: number;
  maxLongitude: number;
}

export interface SearchSheltersCriteria extends BoundingBox {
  limit?: number;
  minRating?: number;
  minCapacity?: number;
  maxCapacity?: number;
}
