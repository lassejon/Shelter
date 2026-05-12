import { apiClient } from '@/shared/api/client';
import type { ResendConfirmationEmailRequest } from '@/features/auth/models/dto';

export async function resendConfirmation(body: ResendConfirmationEmailRequest): Promise<void> {
  await apiClient.post('/api/auth/resend-confirmation', body);
}
