import { apiClient } from '@/shared/api/client';
import type { AuthResponse } from '@/features/auth/models/dto';

export async function upgradeToOwner(): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/api/auth/upgrade-to-owner');
  return data;
}
