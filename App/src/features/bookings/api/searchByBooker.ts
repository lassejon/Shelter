import { apiClient } from '@/shared/api/client';
import type { BookingDetailResponse } from '@/features/bookings/models/dto';

interface SearchByBookerOptions {
  includeHistory?: boolean;
}

export async function searchBookingsByBooker(
  options: SearchByBookerOptions = {},
): Promise<BookingDetailResponse[]> {
  const { data } = await apiClient.get<{ items: BookingDetailResponse[] }>('/api/bookings', {
    params: { IncludeHistory: options.includeHistory ?? false },
  });
  return data.items;
}
