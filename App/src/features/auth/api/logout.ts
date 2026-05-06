import { apiClient } from '@/shared/api/client';

export async function logout(): Promise<void> {
  await apiClient.post('/api/auth/logout');
}
