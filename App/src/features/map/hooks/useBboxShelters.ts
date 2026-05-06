import { useQuery } from '@tanstack/react-query';
import { searchShelters } from '@/features/map/api/searchShelters';
import type { BoundingBox } from '@/features/map/models/dto';

interface UseBboxSheltersOptions {
  bbox: BoundingBox | null;
  filters?: {
    minRating?: number | null;
    minCapacity?: number | null;
    maxCapacity?: number | null;
  };
  /** Party size for the desired booking. Raises the capacity floor; combined with `dates`, drives the
   *  per-shelter availability check on the API (peak concurrent inclusive guests + party ≤ capacity). */
  guests?: number | null;
  /** When both set, the API filters by date availability — see `guests` for the semantic. */
  dates?: { start: Date | null; end: Date | null };
  enabled?: boolean;
}

export function useBboxShelters({
  bbox,
  filters = {},
  guests = null,
  dates,
  enabled = true,
}: UseBboxSheltersOptions) {
  const datesValid = !!(dates?.start && dates?.end && dates.end > dates.start);

  const criteria = bbox
    ? {
        minLatitude: bbox.minLatitude,
        maxLatitude: bbox.maxLatitude,
        minLongitude: bbox.minLongitude,
        maxLongitude: bbox.maxLongitude,
        minRating: filters.minRating ?? undefined,
        minCapacity: filters.minCapacity ?? undefined,
        maxCapacity: filters.maxCapacity ?? undefined,
        guests: guests ?? undefined,
        startUtc: datesValid ? dates!.start!.toISOString() : undefined,
        endUtc: datesValid ? dates!.end!.toISOString() : undefined,
      }
    : null;

  return useQuery({
    queryKey: ['shelters', 'bbox', criteria],
    queryFn: ({ signal }) => searchShelters(criteria!, signal),
    enabled: enabled && criteria !== null,
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
    placeholderData: (previousData) => previousData,
  });
}
