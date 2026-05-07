import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router';
import { Calendar, CheckCircle2, Minus, Plus } from 'lucide-react';
import { addDays, differenceInCalendarDays, format } from 'date-fns';
import type { DateRange } from 'react-day-picker';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import { useMapFilterStore } from '@/shared/stores/map-filter.store';
import { useShelterAvailability } from '@/features/bookings/hooks/useShelterAvailability';
import { useCreateBooking } from '@/features/bookings/hooks/useCreateBooking';
import { BookingDatePicker } from './BookingDatePicker';
import { BookingType, type BookingTypeValue } from '@/features/bookings/models/dto';
import { ShelterBookingPolicy, type ShelterDetailResponse } from '@/features/shelters/models/dto';

interface BookingWidgetProps {
  shelter: ShelterDetailResponse;
}

function policyAllowedTypes(policy: number): BookingTypeValue[] {
  switch (policy) {
    case ShelterBookingPolicy.ExclusiveOnly:
      return [BookingType.Exclusive];
    case ShelterBookingPolicy.InclusiveOnly:
      return [BookingType.Inclusive];
    default:
      return [BookingType.Inclusive, BookingType.Exclusive];
  }
}

export function BookingWidget({ shelter }: BookingWidgetProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  // Prefill from the shared map filter store so values typed into MapHeader carry into the booking flow.
  const filterDates = useMapFilterStore((s) => s.dates);
  const filterGuests = useMapFilterStore((s) => s.guests);

  const [selectedRange, setSelectedRange] = useState<DateRange | undefined>(() =>
    filterDates.start && filterDates.end
      ? { from: filterDates.start, to: filterDates.end }
      : undefined,
  );
  const [guests, setGuests] = useState<number>(filterGuests ?? 1);
  const allowedTypes = useMemo(
    () => policyAllowedTypes(Number(shelter.bookingPolicy)),
    [shelter.bookingPolicy],
  );
  const [bookingType, setBookingType] = useState<BookingTypeValue>(allowedTypes[0]);
  const [maxAvailableGuests, setMaxAvailableGuests] = useState<number>(Number(shelter.capacity));
  const availabilityWindow = useMemo(() => {
    const today = new Date();
    return {
      from: `${format(today, 'yyyy-MM-dd')}T00:00:00Z`,
      to: `${format(addDays(today, 366), 'yyyy-MM-dd')}T00:00:00Z`,
    };
  }, []);

  const {
    data: availabilityBookings = [],
    isLoading: availabilityLoading,
    error: availabilityError,
  } = useShelterAvailability(shelter.id, {
    ...availabilityWindow,
    enabled: isAuthenticated,
  });
  const createMutation = useCreateBooking();

  // Clamp guest count down when availability shrinks (e.g. user picks a partially-booked range).
  // setTimeout(0) plays nicely with React 19's `set-state-in-effect` lint rule (state lands on a
  // microtask boundary instead of directly in the effect body).
  useEffect(() => {
    if (guests <= maxAvailableGuests) return;
    const t = setTimeout(() => setGuests(Math.max(1, maxAvailableGuests)), 0);
    return () => clearTimeout(t);
  }, [maxAvailableGuests, guests]);

  // If the shelter's policy lists only one type, keep state aligned to it.
  useEffect(() => {
    if (allowedTypes.length !== 1) return;
    const t = setTimeout(() => setBookingType(allowedTypes[0]), 0);
    return () => clearTimeout(t);
  }, [allowedTypes]);

  if (!isAuthenticated) {
    return (
      <div className="rounded-lg bg-white p-6 shadow">
        <h2 className="mb-4 text-xl font-semibold text-slate-900">Book this shelter</h2>
        <div className="rounded-lg border border-slate-200 py-8 text-center">
          <Calendar className="mx-auto mb-3 h-12 w-12 text-slate-300" />
          <p className="mb-4 text-slate-600">Please log in to book this shelter.</p>
          <Link
            to="/"
            className="inline-flex items-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700"
          >
            Go home and log in
          </Link>
        </div>
      </div>
    );
  }

  if (createMutation.isSuccess && selectedRange?.from && selectedRange?.to) {
    return (
      <BookingConfirmation
        shelter={shelter}
        from={selectedRange.from}
        to={selectedRange.to}
        guests={guests}
        type={bookingType}
        onBookAnother={() => {
          createMutation.reset();
          setSelectedRange(undefined);
          setGuests(filterGuests ?? 1);
          setBookingType(allowedTypes[0]);
        }}
      />
    );
  }

  const canSubmit =
    Boolean(selectedRange?.from) &&
    Boolean(selectedRange?.to) &&
    !createMutation.isPending &&
    !availabilityError &&
    guests <= maxAvailableGuests &&
    guests >= 1;

  const errorMessage = (() => {
    if (!createMutation.error) return null;
    if (createMutation.error instanceof AxiosError) {
      const data = createMutation.error.response?.data as
        | { detail?: string; title?: string }
        | undefined;
      return data?.detail ?? data?.title ?? createMutation.error.message;
    }
    return createMutation.error.message ?? 'Could not create booking';
  })();

  function handleSubmit() {
    if (!selectedRange?.from || !selectedRange?.to) return;
    createMutation.mutate(
      {
        shelterId: shelter.id,
        body: {
          startUtc: format(selectedRange.from, 'yyyy-MM-dd'),
          endUtc: format(selectedRange.to, 'yyyy-MM-dd'),
          guests,
          type: bookingType,
        },
      },
      {
        // Toast-level acknowledgement; the inline confirmation panel below renders the persistent state.
        onSuccess: () => toast.success('Booking confirmed'),
      },
    );
  }

  return (
    <div className="rounded-lg bg-white p-6 shadow">
      <h2 className="mb-4 text-xl font-semibold text-slate-900">Book this shelter</h2>

      {availabilityLoading ? (
        <div className="animate-pulse space-y-4">
          <div className="h-64 rounded bg-slate-200" />
          <div className="h-10 rounded bg-slate-200" />
        </div>
      ) : availabilityError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4">
          <p className="text-sm font-medium text-red-900">Failed to load availability</p>
          <p className="mt-1 text-sm text-red-700">
            Please refresh the page before choosing dates for this shelter.
          </p>
        </div>
      ) : (
        <>
          <div className="mb-6">
            <BookingDatePicker
              shelter={shelter}
              bookings={availabilityBookings}
              selectedRange={selectedRange}
              onRangeChange={setSelectedRange}
              onCapacityChange={setMaxAvailableGuests}
            />
          </div>

          {allowedTypes.length > 1 && (
            <div className="mb-6">
              <label className="mb-2 block text-sm font-medium text-slate-700">Booking Type</label>
              <div className="grid grid-cols-2 gap-3">
                <BookingTypeOption
                  active={bookingType === BookingType.Exclusive}
                  onClick={() => setBookingType(BookingType.Exclusive)}
                  title="Exclusive"
                  subtitle="Entire shelter for your group"
                />
                <BookingTypeOption
                  active={bookingType === BookingType.Inclusive}
                  onClick={() => setBookingType(BookingType.Inclusive)}
                  title="Shared"
                  subtitle="Share with other groups"
                />
              </div>
            </div>
          )}

          <div className="mb-6">
            <label className="mb-2 block text-sm font-medium text-slate-700">
              Number of guests
            </label>
            <div className="flex items-center gap-4">
              <button
                type="button"
                onClick={() => setGuests(Math.max(1, guests - 1))}
                disabled={guests <= 1}
                aria-label="Decrease guests"
                className="flex h-10 w-10 items-center justify-center rounded-md bg-slate-100 transition-colors hover:bg-slate-200 disabled:bg-slate-50 disabled:text-slate-300"
              >
                <Minus size={18} />
              </button>
              <span className="min-w-[3rem] text-center text-lg font-medium text-slate-900">
                {guests}
              </span>
              <button
                type="button"
                onClick={() => setGuests(Math.min(maxAvailableGuests, guests + 1))}
                disabled={guests >= maxAvailableGuests}
                aria-label="Increase guests"
                className="flex h-10 w-10 items-center justify-center rounded-md bg-slate-100 transition-colors hover:bg-slate-200 disabled:bg-slate-50 disabled:text-slate-300"
              >
                <Plus size={18} />
              </button>
              <span className="text-sm text-slate-600">(Max: {maxAvailableGuests})</span>
            </div>
          </div>

          {selectedRange?.from && selectedRange?.to && (
            <div className="mb-6 rounded-lg border border-slate-200 bg-slate-50 p-4">
              <h3 className="mb-2 text-sm font-semibold text-slate-900">Booking summary</h3>
              <div className="space-y-1 text-sm text-slate-700">
                <p>
                  <span className="font-medium">Type:</span>{' '}
                  {bookingType === BookingType.Exclusive ? 'Exclusive' : 'Shared'}
                </p>
                <p>
                  <span className="font-medium">Check-in:</span>{' '}
                  {format(selectedRange.from, 'MMMM d, yyyy')}
                </p>
                <p>
                  <span className="font-medium">Check-out:</span>{' '}
                  {format(selectedRange.to, 'MMMM d, yyyy')}
                </p>
                <p>
                  <span className="font-medium">Guests:</span> {guests}
                </p>
              </div>
            </div>
          )}

          {selectedRange?.from &&
            selectedRange?.to &&
            maxAvailableGuests < Number(shelter.capacity) && (
              <div className="mb-4 rounded border border-amber-200 bg-amber-50 p-3">
                <p className="text-sm font-medium text-amber-900">⚠️ Limited availability</p>
                <p className="mt-1 text-xs text-amber-700">
                  You can only book up to {maxAvailableGuests}{' '}
                  {maxAvailableGuests === 1 ? 'guest' : 'guests'} for these dates due to existing
                  bookings.
                </p>
              </div>
            )}

          {errorMessage && (
            <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-4">
              <p className="text-sm font-medium text-red-900">Failed to create booking</p>
              <p className="mt-1 text-sm text-red-700">{errorMessage}</p>
            </div>
          )}

          <Button
            type="button"
            variant="primary"
            fullWidth
            onClick={handleSubmit}
            disabled={!canSubmit}
          >
            {createMutation.isPending ? 'Booking…' : 'Book now'}
          </Button>

          {!selectedRange?.from && (
            <p className="mt-2 text-center text-sm text-slate-500">
              Select dates to book this shelter
            </p>
          )}
        </>
      )}
    </div>
  );
}

interface BookingConfirmationProps {
  shelter: ShelterDetailResponse;
  from: Date;
  to: Date;
  guests: number;
  type: BookingTypeValue;
  onBookAnother: () => void;
}

function BookingConfirmation({
  shelter,
  from,
  to,
  guests,
  type,
  onBookAnother,
}: BookingConfirmationProps) {
  const nights = Math.max(1, differenceInCalendarDays(to, from));

  return (
    <div className="rounded-lg bg-white p-8 text-center shadow">
      <div className="mx-auto mb-5 flex h-20 w-20 items-center justify-center rounded-full bg-primary-100">
        <CheckCircle2 className="h-12 w-12 text-primary-600" strokeWidth={2.25} />
      </div>
      <h2 className="mb-2 text-2xl font-bold text-slate-900">Booking confirmed</h2>
      <p className="mb-6 text-slate-600">
        We've reserved your spot at{' '}
        <span className="font-medium text-slate-900">{shelter.name}</span>.
      </p>

      <div className="mx-auto mb-6 max-w-md rounded-lg border border-slate-200 bg-slate-50 p-5 text-left">
        <h3 className="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-500">
          Reservation
        </h3>
        <dl className="space-y-2 text-sm">
          <DetailRow label="Check-in" value={format(from, 'EEE, MMM d, yyyy')} />
          <DetailRow label="Check-out" value={format(to, 'EEE, MMM d, yyyy')} />
          <DetailRow label="Nights" value={String(nights)} />
          <DetailRow label="Guests" value={`${guests} ${guests === 1 ? 'guest' : 'guests'}`} />
          <DetailRow
            label="Type"
            value={type === BookingType.Exclusive ? 'Exclusive (entire shelter)' : 'Shared'}
          />
        </dl>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:justify-center">
        <Link
          to="/settings/bookings"
          className="inline-flex items-center justify-center rounded-md bg-primary-600 px-6 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700"
        >
          View my bookings
        </Link>
        <button
          type="button"
          onClick={onBookAnother}
          className="inline-flex items-center justify-center rounded-md border border-slate-300 bg-white px-6 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50"
        >
          Book another stay
        </button>
      </div>
    </div>
  );
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-3">
      <dt className="text-slate-600">{label}</dt>
      <dd className="font-medium text-slate-900">{value}</dd>
    </div>
  );
}

function BookingTypeOption({
  active,
  onClick,
  title,
  subtitle,
}: {
  active: boolean;
  onClick: () => void;
  title: string;
  subtitle: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-lg border-2 p-3 text-left transition-all ${
        active
          ? 'border-primary-600 bg-primary-50 text-primary-900'
          : 'border-slate-300 hover:border-slate-400'
      }`}
    >
      <div className="text-sm font-medium">{title}</div>
      <div className="mt-1 text-xs text-slate-600">{subtitle}</div>
    </button>
  );
}
