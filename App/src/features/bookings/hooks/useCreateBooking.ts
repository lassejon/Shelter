import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createBooking } from '@/features/bookings/api/createBooking';
import type { BookingDetailResponse, CreateBookingRequest } from '@/features/bookings/models/dto';

interface CreateBookingArgs {
  shelterId: string;
  body: CreateBookingRequest;
}

export function useCreateBooking() {
  const queryClient = useQueryClient();

  return useMutation<BookingDetailResponse, Error, CreateBookingArgs>({
    mutationFn: ({ shelterId, body }) => createBooking(shelterId, body),
    onSuccess: (_data, { shelterId }) => {
      // Invalidate the booker's list and this shelter's bookings so the booking widget,
      // settings page, and bbox availability all refetch.
      queryClient.invalidateQueries({ queryKey: ['bookings', 'me'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'availability', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'shelter', shelterId] });
      queryClient.invalidateQueries({ queryKey: ['shelters', 'bbox'] });
    },
  });
}
