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
      searchShelters(
        {
          minLatitude: -90,
          maxLatitude: 90,
          minLongitude: -180,
          maxLongitude: 180,
          q: trimmed,
          limit: RESULT_LIMIT,
        },
        signal,
      ),
    enabled,
    staleTime: 60 * 1000,
    placeholderData: (previousData) => previousData,
  });

  return {
    results: enabled ? (result.data ?? []) : [],
    isLoading: result.isFetching,
  };
}
