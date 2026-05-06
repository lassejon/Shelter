import { apiClient } from '@/shared/api/client';
import type { AuthResponse, RegisterRequest } from '@/features/auth/models/dto';

export async function register(body: RegisterRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/api/auth/register', body);
  return data;
}
