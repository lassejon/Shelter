import { useQuery } from '@tanstack/react-query';
import { getShelter } from '@/features/shelters/api/getShelter';

export function useShelter(id: string | undefined) {
  return useQuery({
    queryKey: ['shelters', id],
    queryFn: () => getShelter(id!),
    enabled: Boolean(id),
  });
}
