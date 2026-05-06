import { useMutation, useQueryClient } from '@tanstack/react-query';
import { logout as logoutApi } from '@/features/auth/api/logout';
import { useAuthStore } from '@/features/auth/stores/auth.store';

export function useLogout() {
  const queryClient = useQueryClient();
  const logoutStore = useAuthStore((state) => state.logout);

  return useMutation<void, Error, void>({
    mutationFn: async () => {
      try {
        await logoutApi();
      } catch {
        // JWT is stateless — server-side failure shouldn't block the client logout.
      }
    },
    onSettled: () => {
      logoutStore();
      queryClient.clear();
    },
  });
}
