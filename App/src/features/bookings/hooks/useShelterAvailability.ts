import { useQuery } from '@tanstack/react-query';
import { searchBookingAvailability } from '@/features/bookings/api/searchAvailability';

interface UseShelterAvailabilityOptions {
  from?: string;
  to?: string;
  enabled?: boolean;
}

export function useShelterAvailability(
  shelterId: string | undefined,
  { from, to, enabled = true }: UseShelterAvailabilityOptions = {},
) {
  return useQuery({
    queryKey: ['bookings', 'availability', shelterId, { from, to }],
    queryFn: () => searchBookingAvailability(shelterId!, { from, to }),
    enabled: enabled && Boolean(shelterId),
    staleTime: 30 * 1000,
  });
}
