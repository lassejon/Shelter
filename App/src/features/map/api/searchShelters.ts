import { apiClient } from '@/shared/api/client';
import type {
  SearchShelterResponse,
  SearchSheltersCriteria,
} from '@/features/map/models/dto';

export async function searchShelters(
  criteria: SearchSheltersCriteria,
  signal?: AbortSignal,
): Promise<SearchShelterResponse[]> {
  const { data } = await apiClient.get<SearchShelterResponse[]>('/api/shelters', {
    params: criteria,
    signal,
  });
  return data;
}
