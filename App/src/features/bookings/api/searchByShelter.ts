import { apiClient } from '@/shared/api/client';
import type { BookingDetailResponse } from '@/features/bookings/models/dto';

interface SearchByShelterOptions {
  /** ISO timestamps. When unset the API returns all bookings for the shelter. */
  from?: string;
  to?: string;
}

export async function searchBookingsByShelter(
  shelterId: string,
  options: SearchByShelterOptions = {},
): Promise<BookingDetailResponse[]> {
  const { data } = await apiClient.get<BookingDetailResponse[]>(
    `/api/shelters/${shelterId}/bookings`,
    {
      params: {
        From: options.from,
        To: options.to,
      },
    },
  );
  return data;
}
