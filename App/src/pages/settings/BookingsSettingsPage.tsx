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
import { Button, LinkButton } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';

export function BookingsSettingsPage() {
  // Include history so the user can see ended trips with an "Ended" badge.
  const { data: bookings, isLoading, error } = useMyBookings({ includeHistory: true });

  if (isLoading) {
    return (
      <Card className="flex items-center justify-center p-8" padding="none">
        <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="p-8 text-center" padding="none">
        <p className="font-semibold text-red-600">Failed to load bookings</p>
        <p className="mt-1 text-sm text-slate-600">Please try again later.</p>
      </Card>
    );
  }

  if (!bookings || bookings.length === 0) {
    return (
      <Card className="p-8 text-center" padding="none">
        <Calendar className="mx-auto mb-4 h-12 w-12 text-slate-300" />
        <p className="mb-2 text-slate-600">You don't have any bookings yet.</p>
        <p className="mb-6 text-sm text-slate-500">
          Start exploring shelters to make your first booking.
        </p>
        <LinkButton to="/" variant="primary">
          Explore shelters
        </LinkButton>
      </Card>
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
    <Card as="article" className="space-y-4" variant="interactive">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold text-slate-900">{shelter?.name ?? 'Shelter'}</h3>
          <p className="mt-1 text-slate-600">{formatDateRange(booking.startUtc, booking.endUtc)}</p>
          <p className="mt-1 text-sm text-slate-500">
            {Number(booking.guests)} {Number(booking.guests) === 1 ? 'guest' : 'guests'}
          </p>
        </div>
        <BookingStatusBadge status={eff} />
      </div>
      <div className="flex gap-3 pt-2">
        <LinkButton to={`/shelters/${booking.shelterId}`} variant="link" size="inline">
          View shelter
        </LinkButton>
        {canCancel && (
          <Button
            type="button"
            variant="dangerLink"
            size="inline"
            onClick={handleCancel}
            disabled={cancelMutation.isPending}
          >
            {cancelMutation.isPending ? 'Cancelling…' : 'Cancel booking'}
          </Button>
        )}
      </div>
    </Card>
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
