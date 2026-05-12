import { apiClient } from '@/shared/api/client';
import type { AuthResponse, ConfirmEmailRequest } from '@/features/auth/models/dto';

export async function confirmEmail(body: ConfirmEmailRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/api/auth/confirm-email', body);
  return data;
}
