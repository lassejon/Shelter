import { apiClient } from '@/shared/api/client';
import type {
  BookingDetailResponse,
  CreateBookingRequest,
} from '@/features/bookings/models/dto';

export async function createBooking(
  shelterId: string,
  body: CreateBookingRequest,
): Promise<BookingDetailResponse> {
  const { data } = await apiClient.post<BookingDetailResponse>(
    `/api/shelters/${shelterId}/bookings`,
    body,
  );
  return data;
}
