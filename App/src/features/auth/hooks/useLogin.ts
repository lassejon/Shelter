import { useMutation } from '@tanstack/react-query';
import { login } from '@/features/auth/api/login';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import type { AuthResponse, LoginRequest } from '@/features/auth/models/dto';

export function useLogin() {
  const setAuth = useAuthStore((state) => state.setAuth);

  return useMutation<AuthResponse, Error, LoginRequest>({
    mutationFn: login,
    onSuccess: (response) => {
      setAuth(response);
    },
  });
}
