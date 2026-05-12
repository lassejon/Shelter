import { useMutation } from '@tanstack/react-query';
import { resendConfirmation } from '@/features/auth/api/resendConfirmation';
import type { ResendConfirmationEmailRequest } from '@/features/auth/models/dto';

export function useResendConfirmation() {
  return useMutation<void, Error, ResendConfirmationEmailRequest>({
    mutationFn: resendConfirmation,
  });
}
