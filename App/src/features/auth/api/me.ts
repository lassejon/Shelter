import { apiClient } from '@/shared/api/client';
import type { AuthResponse } from '@/features/auth/models/dto';

export async function fetchMe(): Promise<AuthResponse> {
  const { data } = await apiClient.get<AuthResponse>('/api/auth/me');
  return data;
}
