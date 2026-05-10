import { apiClient } from '@/shared/api/client';
import type { BookingDetailResponse } from '@/features/bookings/models/dto';

export async function approveBooking(bookingId: string): Promise<BookingDetailResponse> {
  const { data } = await apiClient.post<BookingDetailResponse>(
    `/api/bookings/${bookingId}/approve`,
  );
  return data;
}
