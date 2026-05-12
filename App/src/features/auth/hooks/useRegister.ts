import { useMutation } from '@tanstack/react-query';
import { register } from '@/features/auth/api/register';
import type { RegisterRequest, RegisterResponse } from '@/features/auth/models/dto';

/**
 * Submits the register form. Unlike the prior auto-login flow, the response carries no JWT —
 * the user must click the confirmation link in their email before they can log in. The form
 * redirects to the "check your email" page on success.
 */
export function useRegister() {
  return useMutation<RegisterResponse, Error, RegisterRequest>({
    mutationFn: register,
  });
}
