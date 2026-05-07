import { apiClient } from '@/shared/api/client';

export async function cancelBooking(bookingId: string): Promise<void> {
  await apiClient.delete(`/api/bookings/${bookingId}`);
}
