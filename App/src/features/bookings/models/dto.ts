import type { components } from '@/shared/api/types/paths';

export type BookingDetailResponse = components['schemas']['BookingDetailResponse'];
export type CreateBookingRequest = components['schemas']['CreateBookingRequest'];

export const BookingType = {
  Inclusive: 0,
  Exclusive: 1,
} as const;

export type BookingTypeValue = (typeof BookingType)[keyof typeof BookingType];

// Match Shelter.Domain.Bookings.BookingStatus — three values, not four.
export const BookingStatus = {
  Pending: 0,
  Confirmed: 1,
  Cancelled: 2,
} as const;

export type BookingStatusValue = (typeof BookingStatus)[keyof typeof BookingStatus];

const typeLabels: Record<BookingTypeValue, string> = {
  [BookingType.Inclusive]: 'Shared',
  [BookingType.Exclusive]: 'Exclusive',
};

const statusLabels: Record<BookingStatusValue, string> = {
  [BookingStatus.Pending]: 'Pending',
  [BookingStatus.Confirmed]: 'Confirmed',
  [BookingStatus.Cancelled]: 'Cancelled',
};

export function bookingTypeLabel(type: number): string {
  return typeLabels[type as BookingTypeValue] ?? 'Unknown';
}

export function bookingStatusLabel(status: number): string {
  return statusLabels[status as BookingStatusValue] ?? 'Unknown';
}
