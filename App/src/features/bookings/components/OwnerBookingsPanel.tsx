import { useMemo } from 'react';
import { Loader2 } from 'lucide-react';
import { useShelterBookings } from '@/features/bookings/hooks/useShelterBookings';
import { bookingTypeLabel } from '@/features/bookings/models/dto';
import {
  effectiveBookingStatus,
  effectiveBookingStatusLabel,
  effectiveBookingStatusStyles,
} from '@/features/bookings/models/effectiveStatus';
import { formatDateRange } from '@/shared/utils/date';

interface OwnerBookingsPanelProps {
  shelterId: string;
}

export function OwnerBookingsPanel({ shelterId }: OwnerBookingsPanelProps) {
  const { data: bookings, isLoading, error } = useShelterBookings(shelterId);

  // Owners care about *now* and *future*: drop ended and cancelled bookings entirely.
  // The user's own settings page is where past trips and cancellations are visible.
  const visible = useMemo(() => {
    if (!bookings) return [];
    const now = new Date();
    return bookings
      .map((b) => ({ b, eff: effectiveBookingStatus(b, now) }))
      .filter(({ eff }) => eff !== 'ended' && eff !== 'cancelled');
  }, [bookings]);

  return (
    <div className="rounded-lg bg-white p-6 shadow">
      <div className="mb-4 flex items-baseline justify-between">
        <h2 className="text-xl font-semibold text-slate-900">Bookings on this shelter</h2>
        <span className="text-xs uppercase tracking-wide text-slate-500">Owner view</span>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center p-6">
          <Loader2 className="h-6 w-6 animate-spin text-primary-600" />
        </div>
      )}

      {error && <p className="text-sm text-red-600">Failed to load bookings.</p>}

      {!isLoading && !error && visible.length === 0 && (
        <p className="text-sm text-slate-500">No upcoming or current bookings.</p>
      )}

      {!isLoading && visible.length > 0 && (
        <ul className="divide-y divide-slate-200">
          {visible.map(({ b, eff }) => (
            <li key={b.id} className="flex items-start justify-between gap-3 py-3">
              <div>
                <p className="font-medium text-slate-900">
                  {b.bookerName ?? 'Anonymous booker'}
                </p>
                <p className="text-sm text-slate-600">
                  {formatDateRange(b.startUtc, b.endUtc)} · {Number(b.guests)}{' '}
                  {Number(b.guests) === 1 ? 'guest' : 'guests'} ·{' '}
                  {bookingTypeLabel(Number(b.type))}
                </p>
              </div>
              <span
                className={`shrink-0 rounded-full px-3 py-1 text-xs font-medium ${effectiveBookingStatusStyles[eff]}`}
              >
                {effectiveBookingStatusLabel(eff)}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
