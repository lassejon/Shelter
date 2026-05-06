import { apiClient } from '@/shared/api/client';
import type { ShelterDetailResponse } from '@/features/shelters/models/dto';

export async function getShelter(id: string): Promise<ShelterDetailResponse> {
  const { data } = await apiClient.get<ShelterDetailResponse>(`/api/shelters/${id}`);
  return data;
}
