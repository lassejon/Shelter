import { useEffect, useState } from 'react';
import { getPlacePredictions, type PlacePrediction } from '@/features/search/services/places';

interface UsePlacePredictionsResult {
  results: PlacePrediction[];
  isLoading: boolean;
}

/** Caller is responsible for debouncing `query` (e.g. via shared/hooks/useDebouncedValue). */
export function usePlacePredictions(
  query: string,
  locationBias?: { lat: number; lng: number },
): UsePlacePredictionsResult {
  const [results, setResults] = useState<PlacePrediction[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const lat = locationBias?.lat;
  const lng = locationBias?.lng;

  useEffect(() => {
    const trimmed = query.trim();
    if (trimmed.length < 2) {
      const t = setTimeout(() => {
        setResults([]);
        setIsLoading(false);
      }, 0);
      return () => clearTimeout(t);
    }
    let cancelled = false;
    const t = setTimeout(async () => {
      setIsLoading(true);
      const bias = lat !== undefined && lng !== undefined ? { lat, lng } : undefined;
      const predictions = await getPlacePredictions(trimmed, bias);
      if (cancelled) return;
      setResults(predictions);
      setIsLoading(false);
    }, 0);
    return () => {
      cancelled = true;
      clearTimeout(t);
    };
  }, [query, lat, lng]);

  return { results, isLoading };
}
