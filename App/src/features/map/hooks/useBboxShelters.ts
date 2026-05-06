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
  enabled?: boolean;
}

export function useBboxShelters({
  bbox,
  filters = {},
  enabled = true,
}: UseBboxSheltersOptions) {
  const criteria = bbox
    ? {
        minLatitude: bbox.minLatitude,
        maxLatitude: bbox.maxLatitude,
        minLongitude: bbox.minLongitude,
        maxLongitude: bbox.maxLongitude,
        minRating: filters.minRating ?? undefined,
        minCapacity: filters.minCapacity ?? undefined,
        maxCapacity: filters.maxCapacity ?? undefined,
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
