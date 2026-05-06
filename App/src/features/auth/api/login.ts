import { apiClient } from '@/shared/api/client';
import type { AuthResponse, LoginRequest } from '@/features/auth/models/dto';

export async function login(body: LoginRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/api/auth/login', body);
  return data;
}
