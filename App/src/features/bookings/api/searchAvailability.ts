import { apiClient } from '@/shared/api/client';
import type { BookingAvailabilityResponse } from '@/features/bookings/models/dto';

interface SearchAvailabilityOptions {
  /** ISO timestamps. When unset the API returns the default booking horizon. */
  from?: string;
  to?: string;
}

export async function searchBookingAvailability(
  shelterId: string,
  options: SearchAvailabilityOptions = {},
): Promise<BookingAvailabilityResponse[]> {
  const { data } = await apiClient.get<{ items: BookingAvailabilityResponse[] }>(
    `/api/shelters/${shelterId}/bookings/availability`,
    {
      params: {
        From: options.from,
        To: options.to,
      },
    },
  );
  return data.items;
}
