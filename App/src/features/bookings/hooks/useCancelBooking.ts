import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cancelBooking } from '@/features/bookings/api/cancelBooking';

export function useCancelBooking() {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: (bookingId) => cancelBooking(bookingId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookings', 'me'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'availability'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'shelter'] });
      queryClient.invalidateQueries({ queryKey: ['shelters', 'bbox'] });
    },
  });
}
