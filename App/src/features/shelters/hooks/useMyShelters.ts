import { useQuery } from '@tanstack/react-query';
import { getMyShelters } from '@/features/shelters/api/getMyShelters';

export function useMyShelters() {
  return useQuery({
    queryKey: ['shelters', 'mine'],
    queryFn: getMyShelters,
    staleTime: 30 * 1000,
  });
}
