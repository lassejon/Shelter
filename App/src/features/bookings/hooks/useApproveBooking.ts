import { useMutation, useQueryClient } from '@tanstack/react-query';
import { approveBooking } from '@/features/bookings/api/approveBooking';
import type { BookingDetailResponse } from '@/features/bookings/models/dto';

export function useApproveBooking() {
  const queryClient = useQueryClient();

  return useMutation<BookingDetailResponse, Error, string>({
    mutationFn: approveBooking,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookings', 'me'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'shelter'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'availability'] });
    },
  });
}
