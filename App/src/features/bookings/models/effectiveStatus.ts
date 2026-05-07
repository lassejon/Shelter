import {
  BookingStatus,
  type BookingDetailResponse,
  type BookingStatusValue,
} from '@/features/bookings/models/dto';

/**
 * Status as the UI presents it: the API only stores Pending / Confirmed / Cancelled,
 * but the UX wants to distinguish a future booking from an ongoing stay from a past trip.
 * "ongoing" and "ended" are derived from the booking's date range; the underlying API
 * status stays Confirmed throughout.
 */
export type EffectiveBookingStatus =
  | 'cancelled'
  | 'pending'
  | 'confirmed'
  | 'ongoing'
  | 'ended';

export function effectiveBookingStatus(
  booking: BookingDetailResponse,
  now: Date = new Date(),
): EffectiveBookingStatus {
  const status = Number(booking.status) as BookingStatusValue;
  if (status === BookingStatus.Cancelled) return 'cancelled';
  if (status === BookingStatus.Pending) return 'pending';

  // Confirmed: split by date.
  const start = new Date(booking.startUtc);
  const end = new Date(booking.endUtc);
  if (now < start) return 'confirmed';
  if (now < end) return 'ongoing';
  return 'ended';
}

const labels: Record<EffectiveBookingStatus, string> = {
  cancelled: 'Cancelled',
  pending: 'Pending',
  confirmed: 'Confirmed',
  ongoing: 'On going',
  ended: 'Ended',
};

export function effectiveBookingStatusLabel(status: EffectiveBookingStatus): string {
  return labels[status];
}

/** Tailwind classes for badges, keyed by effective status. */
export const effectiveBookingStatusStyles: Record<EffectiveBookingStatus, string> = {
  cancelled: 'bg-red-100 text-red-800',
  pending: 'bg-amber-100 text-amber-800',
  confirmed: 'bg-primary-100 text-primary-800',
  // sky distinguishes "happening right now" from upcoming primary green.
  ongoing: 'bg-sky-100 text-sky-800',
  ended: 'bg-slate-100 text-slate-700',
};

/** True for statuses that the booker can still cancel client-side. */
export function isCancellable(status: EffectiveBookingStatus): boolean {
  return status === 'pending' || status === 'confirmed';
}
