import type { components } from '@/shared/api/types/paths';

export type SearchShelterResponse = components['schemas']['SearchShelterResponse'];

export interface BoundingBox {
  minLatitude: number;
  maxLatitude: number;
  minLongitude: number;
  maxLongitude: number;
}

export interface SearchSheltersCriteria extends Partial<BoundingBox> {
  // bbox fields are optional: the backend's spatial filter is conditional on all four
  // being supplied, so callers that only want to search by name can omit them entirely.
  // Avoid sending a near-worldwide bbox as a stand-in for "no filter" — PostGIS
  // geography intersection rejects it as antipodal.
  limit?: number;
  minRating?: number;
  minCapacity?: number;
  maxCapacity?: number;
  guests?: number;
  startUtc?: string;
  endUtc?: string;
  /** Free-text trigram query against shelter Name. */
  q?: string;
}
