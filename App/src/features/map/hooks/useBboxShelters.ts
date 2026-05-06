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
  /** Number of guests; raises the effective minimum capacity. */
  guests?: number | null;
  /** When both set, the API excludes shelters with overlapping non-cancelled bookings. */
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
  // Effective minCapacity: at minimum the requested guest count, but the explicit filter
  // wins when it's stricter. ⌐ both null  → undefined.
  const explicitMin = filters.minCapacity ?? null;
  const effectiveMinCapacity =
    explicitMin !== null && guests !== null
      ? Math.max(explicitMin, guests)
      : (explicitMin ?? guests ?? undefined);

  const datesValid = !!(dates?.start && dates?.end && dates.end > dates.start);

  const criteria = bbox
    ? {
        minLatitude: bbox.minLatitude,
        maxLatitude: bbox.maxLatitude,
        minLongitude: bbox.minLongitude,
        maxLongitude: bbox.maxLongitude,
        minRating: filters.minRating ?? undefined,
        minCapacity: effectiveMinCapacity ?? undefined,
        maxCapacity: filters.maxCapacity ?? undefined,
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
