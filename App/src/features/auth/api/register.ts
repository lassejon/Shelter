import { apiClient } from '@/shared/api/client';
import type { RegisterRequest, RegisterResponse } from '@/features/auth/models/dto';

export async function register(body: RegisterRequest): Promise<RegisterResponse> {
  const { data } = await apiClient.post<RegisterResponse>('/api/auth/register', body);
  return data;
}
