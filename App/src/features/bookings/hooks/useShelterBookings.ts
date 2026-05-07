import { useQuery } from '@tanstack/react-query';
import { searchBookingsByShelter } from '@/features/bookings/api/searchByShelter';

interface UseShelterBookingsOptions {
  from?: string;
  to?: string;
  enabled?: boolean;
}

export function useShelterBookings(
  shelterId: string | undefined,
  { from, to, enabled = true }: UseShelterBookingsOptions = {},
) {
  return useQuery({
    queryKey: ['bookings', 'shelter', shelterId, { from, to }],
    queryFn: () => searchBookingsByShelter(shelterId!, { from, to }),
    enabled: enabled && Boolean(shelterId),
    staleTime: 30 * 1000,
  });
}
