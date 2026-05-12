import { apiClient } from '@/shared/api/client';
import type { ShelterDetailResponse } from '@/features/shelters/models/dto';

export async function getMyShelters(): Promise<ShelterDetailResponse[]> {
  const { data } = await apiClient.get<{ items: ShelterDetailResponse[] }>('/api/shelters/mine');
  return data.items;
}
