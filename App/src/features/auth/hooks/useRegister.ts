import { useMutation } from '@tanstack/react-query';
import { register } from '@/features/auth/api/register';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import type { AuthResponse, RegisterRequest } from '@/features/auth/models/dto';

export function useRegister() {
  const setAuth = useAuthStore((state) => state.setAuth);

  return useMutation<AuthResponse, Error, RegisterRequest>({
    mutationFn: register,
    onSuccess: (response) => {
      setAuth(response);
    },
  });
}
