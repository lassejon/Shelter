import { Link } from 'react-router';
import { Calendar, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { useMyBookings } from '@/features/bookings/hooks/useMyBookings';
import { useCancelBooking } from '@/features/bookings/hooks/useCancelBooking';
import { useShelter } from '@/features/shelters/hooks/useShelter';
import type { BookingDetailResponse } from '@/features/bookings/models/dto';
import {
  effectiveBookingStatus,
  effectiveBookingStatusLabel,
  effectiveBookingStatusStyles,
  isCancellable,
  type EffectiveBookingStatus,
} from '@/features/bookings/models/effectiveStatus';
import { formatDateRange } from '@/shared/utils/date';

export function BookingsSettingsPage() {
  // Include history so the user can see ended trips with an "Ended" badge.
  const { data: bookings, isLoading, error } = useMyBookings({ includeHistory: true });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center">
        <p className="font-semibold text-red-600">Failed to load bookings</p>
        <p className="mt-1 text-sm text-slate-600">Please try again later.</p>
      </div>
    );
  }

  if (!bookings || bookings.length === 0) {
    return (
      <div className="p-8 text-center">
        <Calendar className="mx-auto mb-4 h-12 w-12 text-slate-300" />
        <p className="mb-2 text-slate-600">You don't have any bookings yet.</p>
        <p className="mb-6 text-sm text-slate-500">
          Start exploring shelters to make your first booking.
        </p>
        <Link
          to="/"
          className="inline-flex items-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700"
        >
          Explore shelters
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="mb-2 text-3xl font-semibold text-slate-900">My Bookings</h1>
        <p className="text-slate-600">View and manage your shelter bookings.</p>
      </div>
      <div className="space-y-4">
        {bookings.map((booking) => (
          <BookingCard key={booking.id} booking={booking} />
        ))}
      </div>
    </div>
  );
}

function BookingCard({ booking }: { booking: BookingDetailResponse }) {
  // Pull shelter name from cache; if not cached, this fires a background fetch.
  const { data: shelter } = useShelter(booking.shelterId);
  const cancelMutation = useCancelBooking();

  const eff = effectiveBookingStatus(booking);
  const canCancel = isCancellable(eff);

  function handleCancel() {
    if (!window.confirm('Cancel this booking? This cannot be undone.')) return;
    cancelMutation.mutate(booking.id, {
      onSuccess: () => toast.success('Booking cancelled'),
      onError: () => toast.error('Could not cancel booking'),
    });
  }

  return (
    <div className="space-y-4 rounded-lg border border-slate-200 p-6 transition-shadow hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold text-slate-900">
            {shelter?.name ?? 'Shelter'}
          </h3>
          <p className="mt-1 text-slate-600">
            {formatDateRange(booking.startUtc, booking.endUtc)}
          </p>
          <p className="mt-1 text-sm text-slate-500">
            {Number(booking.guests)} {Number(booking.guests) === 1 ? 'guest' : 'guests'}
          </p>
        </div>
        <BookingStatusBadge status={eff} />
      </div>
      <div className="flex gap-3 pt-2">
        <Link
          to={`/shelters/${booking.shelterId}`}
          className="text-sm font-medium text-primary-600 transition-colors hover:text-primary-700"
        >
          View shelter
        </Link>
        {canCancel && (
          <button
            type="button"
            onClick={handleCancel}
            disabled={cancelMutation.isPending}
            className="text-sm font-medium text-red-600 transition-colors hover:text-red-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {cancelMutation.isPending ? 'Cancelling…' : 'Cancel booking'}
          </button>
        )}
      </div>
    </div>
  );
}

function BookingStatusBadge({ status }: { status: EffectiveBookingStatus }) {
  return (
    <span
      className={`rounded-full px-3 py-1 text-sm font-medium ${effectiveBookingStatusStyles[status]}`}
    >
      {effectiveBookingStatusLabel(status)}
    </span>
  );
}
