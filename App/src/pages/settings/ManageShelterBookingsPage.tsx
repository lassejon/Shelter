import { useMemo } from 'react';
import { Link, useParams } from 'react-router';
import { ArrowLeft, Calendar, Check, Loader2, X } from 'lucide-react';
import { toast } from 'sonner';
import { useShelter } from '@/features/shelters/hooks/useShelter';
import { useShelterBookings } from '@/features/bookings/hooks/useShelterBookings';
import { useApproveBooking } from '@/features/bookings/hooks/useApproveBooking';
import { useCancelBooking } from '@/features/bookings/hooks/useCancelBooking';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { formatDateRange } from '@/shared/utils/date';
import { bookingTypeLabel } from '@/features/bookings/models/dto';
import {
  effectiveBookingStatus,
  effectiveBookingStatusLabel,
  effectiveBookingStatusStyles,
  type EffectiveBookingStatus,
} from '@/features/bookings/models/effectiveStatus';
import type { BookingDetailResponse } from '@/features/bookings/models/dto';

const STATUS_ORDER: Record<EffectiveBookingStatus, number> = {
  pending: 0,
  ongoing: 1,
  confirmed: 2,
  ended: 3,
  cancelled: 4,
};

export function ManageShelterBookingsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: shelter } = useShelter(id);
  const { data: bookings, isLoading, error } = useShelterBookings(id);

  const sorted = useMemo(() => {
    if (!bookings) return [];
    const now = new Date();
    return bookings
      .map((b) => ({ b, eff: effectiveBookingStatus(b, now) }))
      .sort((a, b) => {
        const byStatus = STATUS_ORDER[a.eff] - STATUS_ORDER[b.eff];
        if (byStatus !== 0) return byStatus;
        return new Date(a.b.startUtc).getTime() - new Date(b.b.startUtc).getTime();
      });
  }, [bookings]);

  if (isLoading) {
    return (
      <Card className="flex items-center justify-center p-8" padding="none">
        <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
      </Card>
    );
  }
  if (error || !id) {
    return (
      <Card className="p-8 text-center" padding="none">
        <p className="font-semibold text-red-600">Failed to load bookings</p>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <Link
          to={`/settings/shelters/${id}`}
          className="mb-3 inline-flex items-center gap-2 text-sm font-medium text-primary-600 hover:text-primary-700"
        >
          <ArrowLeft size={18} />
          {shelter?.name ?? 'Shelter'}
        </Link>
        <h1 className="text-3xl font-semibold text-slate-900">Bookings</h1>
        <p className="mt-1 text-slate-600">
          Approve pending bookings, or cancel bookings you can't honour.
        </p>
      </div>

      {sorted.length === 0 ? (
        <Card className="p-8 text-center" padding="none">
          <Calendar className="mx-auto mb-4 h-12 w-12 text-slate-300" />
          <p className="text-slate-600">No bookings yet on this shelter.</p>
        </Card>
      ) : (
        <div className="space-y-3">
          {sorted.map(({ b, eff }) => (
            <BookingRow key={String(b.id)} booking={b} eff={eff} />
          ))}
        </div>
      )}
    </div>
  );
}

function BookingRow({
  booking,
  eff,
}: {
  booking: BookingDetailResponse;
  eff: EffectiveBookingStatus;
}) {
  const approveMutation = useApproveBooking();
  const cancelMutation = useCancelBooking();

  const isPending = eff === 'pending';
  const isFuture = eff === 'pending' || eff === 'confirmed';
  const cancelLabel = isPending ? 'Reject' : 'Cancel';
  const busy = approveMutation.isPending || cancelMutation.isPending;

  function handleApprove() {
    approveMutation.mutate(String(booking.id), {
      onSuccess: () => toast.success('Booking approved'),
      onError: () => toast.error('Could not approve booking'),
    });
  }

  function handleCancel() {
    const verb = isPending ? 'Reject' : 'Cancel';
    if (!window.confirm(`${verb} this booking? The booker will be notified.`)) return;
    cancelMutation.mutate(String(booking.id), {
      onSuccess: () => toast.success(isPending ? 'Booking rejected' : 'Booking cancelled'),
      onError: () => toast.error('Could not cancel booking'),
    });
  }

  return (
    <Card as="article" padding="md" variant="interactive" className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="font-medium text-slate-900">{booking.bookerName ?? 'Anonymous booker'}</p>
          <p className="text-sm text-slate-600">
            {formatDateRange(booking.startUtc, booking.endUtc)} · {Number(booking.guests)}{' '}
            {Number(booking.guests) === 1 ? 'guest' : 'guests'} ·{' '}
            {bookingTypeLabel(Number(booking.type))}
          </p>
        </div>
        <span
          className={`shrink-0 rounded-full px-3 py-1 text-xs font-medium ${effectiveBookingStatusStyles[eff]}`}
        >
          {effectiveBookingStatusLabel(eff)}
        </span>
      </div>
      {isFuture && (
        <div className="flex flex-wrap gap-2 pt-1">
          {isPending && (
            <Button
              type="button"
              variant="primary"
              size="sm"
              onClick={handleApprove}
              disabled={busy}
            >
              <Check size={16} />
              {approveMutation.isPending ? 'Approving…' : 'Approve'}
            </Button>
          )}
          <Button
            type="button"
            variant="dangerOutline"
            size="sm"
            onClick={handleCancel}
            disabled={busy}
          >
            <X size={16} />
            {cancelMutation.isPending ? 'Working…' : cancelLabel}
          </Button>
        </div>
      )}
    </Card>
  );
}
