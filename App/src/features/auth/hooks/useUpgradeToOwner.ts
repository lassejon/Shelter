import { useMutation } from '@tanstack/react-query';
import { upgradeToOwner } from '@/features/auth/api/upgradeToOwner';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import type { AuthResponse } from '@/features/auth/models/dto';

export function useUpgradeToOwner() {
  const setAuth = useAuthStore((state) => state.setAuth);

  return useMutation<AuthResponse, Error, void>({
    mutationFn: upgradeToOwner,
    onSuccess: (response) => {
      setAuth(response);
    },
  });
}
