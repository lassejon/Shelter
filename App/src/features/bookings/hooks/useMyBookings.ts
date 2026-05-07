import { useQuery } from '@tanstack/react-query';
import { searchBookingsByBooker } from '@/features/bookings/api/searchByBooker';

interface UseMyBookingsOptions {
  includeHistory?: boolean;
}

export function useMyBookings({ includeHistory = false }: UseMyBookingsOptions = {}) {
  return useQuery({
    queryKey: ['bookings', 'me', { includeHistory }],
    queryFn: () => searchBookingsByBooker({ includeHistory }),
    staleTime: 60 * 1000,
  });
}
