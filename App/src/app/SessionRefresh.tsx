import { useMe } from '@/features/auth/hooks/useMe';

export function SessionRefresh() {
  useMe();
  return null;
}
