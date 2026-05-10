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
import {
  BookingStatus,
  bookingTypeLabel,
  type BookingDetailResponse,
  type BookingStatusValue,
} from '@/features/bookings/models/dto';
import {
  effectiveBookingStatus,
  effectiveBookingStatusLabel,
  effectiveBookingStatusStyles,
  type EffectiveBookingStatus,
} from '@/features/bookings/models/effectiveStatus';

type SectionKey = 'pending' | 'confirmed' | 'cancelled';

const SECTION_DEFS: { key: SectionKey; status: BookingStatusValue; title: string }[] = [
  { key: 'pending', status: BookingStatus.Pending, title: 'Pending' },
  { key: 'confirmed', status: BookingStatus.Confirmed, title: 'Confirmed' },
  { key: 'cancelled', status: BookingStatus.Cancelled, title: 'Cancelled' },
];

export function ManageShelterBookingsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: shelter } = useShelter(id);
  const { data: bookings, isLoading, error } = useShelterBookings(id);

  const grouped = useMemo(() => {
    const buckets: Record<SectionKey, BookingDetailResponse[]> = {
      pending: [],
      confirmed: [],
      cancelled: [],
    };
    if (!bookings) return buckets;
    // Backend returns Pending → Confirmed → Cancelled, then StartUtc desc within each.
    // Preserve that order by pushing into the matching bucket as we iterate.
    for (const booking of bookings) {
      const status = Number(booking.status) as BookingStatusValue;
      if (status === BookingStatus.Pending) buckets.pending.push(booking);
      else if (status === BookingStatus.Confirmed) buckets.confirmed.push(booking);
      else if (status === BookingStatus.Cancelled) buckets.cancelled.push(booking);
    }
    return buckets;
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

  const totalCount = grouped.pending.length + grouped.confirmed.length + grouped.cancelled.length;

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

      {totalCount === 0 ? (
        <Card className="p-8 text-center" padding="none">
          <Calendar className="mx-auto mb-4 h-12 w-12 text-slate-300" />
          <p className="text-slate-600">No bookings yet on this shelter.</p>
        </Card>
      ) : (
        <div className="space-y-8">
          {SECTION_DEFS.filter((s) => grouped[s.key].length > 0).map((section) => (
            <BookingsSection
              key={section.key}
              title={section.title}
              bookings={grouped[section.key]}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function BookingsSection({
  title,
  bookings,
}: {
  title: string;
  bookings: BookingDetailResponse[];
}) {
  return (
    <section>
      <div className="mb-3 flex items-baseline gap-3">
        <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
        <span className="text-sm text-slate-500">
          {bookings.length} {bookings.length === 1 ? 'booking' : 'bookings'}
        </span>
      </div>
      <div className="space-y-3">
        {bookings.map((booking) => (
          <BookingRow key={String(booking.id)} booking={booking} />
        ))}
      </div>
    </section>
  );
}

function BookingRow({ booking }: { booking: BookingDetailResponse }) {
  const eff: EffectiveBookingStatus = effectiveBookingStatus(booking);
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
