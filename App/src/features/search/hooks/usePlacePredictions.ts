import { useEffect, useState } from 'react';
import { getPlacePredictions, type PlacePrediction } from '@/features/search/services/places';

interface UsePlacePredictionsResult {
  results: PlacePrediction[];
  isLoading: boolean;
}

export function usePlacePredictions(
  query: string,
  locationBias?: { lat: number; lng: number },
  debounceMs = 300,
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
    const timer = setTimeout(async () => {
      setIsLoading(true);
      const bias = lat !== undefined && lng !== undefined ? { lat, lng } : undefined;
      const predictions = await getPlacePredictions(trimmed, bias);
      setResults(predictions);
      setIsLoading(false);
    }, debounceMs);
    return () => clearTimeout(timer);
  }, [query, lat, lng, debounceMs]);

  return { results, isLoading };
}
