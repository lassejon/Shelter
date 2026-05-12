import { useMutation, useQueryClient } from '@tanstack/react-query';
import { confirmEmail } from '@/features/auth/api/confirmEmail';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import type { AuthResponse, ConfirmEmailRequest } from '@/features/auth/models/dto';

/**
 * Confirms the user's email and auto-logs in. The API returns a fresh AuthResponse on success,
 * which we store in the auth store so the user lands signed in.
 */
export function useConfirmEmail() {
  const setAuth = useAuthStore((state) => state.setAuth);
  const queryClient = useQueryClient();

  return useMutation<AuthResponse, Error, ConfirmEmailRequest>({
    mutationFn: confirmEmail,
    onSuccess: (response) => {
      setAuth(response);
      queryClient.invalidateQueries();
    },
  });
}
