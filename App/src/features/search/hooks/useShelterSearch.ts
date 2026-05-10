import { useQuery } from '@tanstack/react-query';
import { searchShelters } from '@/features/map/api/searchShelters';
import type { SearchShelterResponse } from '@/features/map/models/dto';

const RESULT_LIMIT = 10;
const MIN_QUERY_LENGTH = 2;

interface UseShelterSearchResult {
  results: SearchShelterResponse[];
  isLoading: boolean;
}

/** Caller is responsible for debouncing `query` (e.g. via shared/hooks/useDebouncedValue). */
export function useShelterSearch(query: string): UseShelterSearchResult {
  const trimmed = query.trim();
  const enabled = trimmed.length >= MIN_QUERY_LENGTH;

  const result = useQuery({
    queryKey: ['shelters', 'search', trimmed],
    queryFn: ({ signal }) =>
      // No bbox: the search bar is "find by name", not "find within a viewport". Backend
      // skips the spatial filter when any of the four lat/lng params is missing.
      // Previously we sent `(-180, -90, 180, 90)` to act as "worldwide", but PostGIS
      // geography intersection rejects polygons that span the antimeridian
      // ("Antipodal (180 degrees long) edge detected"), so this hook silently returned
      // empty for every query.
      searchShelters({ q: trimmed, limit: RESULT_LIMIT }, signal),
    enabled,
    staleTime: 60 * 1000,
    placeholderData: (previousData) => previousData,
  });

  return {
    results: enabled ? (result.data ?? []) : [],
    isLoading: result.isFetching,
  };
}
