import { useCallback, useEffect, useMemo, useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import {
  addDays,
  addMonths,
  eachDayOfInterval,
  endOfMonth,
  format,
  isBefore,
  isSameDay,
  isSameMonth,
  isWithinInterval,
  parseISO,
  startOfDay,
  startOfMonth,
  subMonths,
} from 'date-fns';
import type { DateRange } from 'react-day-picker';
import {
  BookingStatus,
  BookingType,
  type BookingDetailResponse,
} from '@/features/bookings/models/dto';
import {
  ShelterBookingPolicy,
  type ShelterDetailResponse,
} from '@/features/shelters/models/dto';

interface BookingDatePickerProps {
  shelter: ShelterDetailResponse;
  bookings: BookingDetailResponse[];
  selectedRange: DateRange | undefined;
  onRangeChange: (range: DateRange | undefined) => void;
  onCapacityChange?: (maxCapacity: number) => void;
}

type HalfStatus = 'available' | 'partial' | 'full';

/**
 * Per-day availability split into two halves:
 *  - top-left  = check-out (morning of D, ending night D-1 → D)
 *  - bottom-right = check-in (evening of D, starting night D → D+1)
 *
 * A booking from Jan 3 → Jan 5 occupies:
 *  - Jan 3: bottom-right (arriving)
 *  - Jan 4: both halves (staying overnight)
 *  - Jan 5: top-left (leaving)
 */
interface DayAvailability {
  checkoutStatus: HalfStatus;
  checkinStatus: HalfStatus;
  checkoutGuests: number;
  checkinGuests: number;
  availableCapacityForCheckin: number;
}

function calculateAvailability(
  shelter: ShelterDetailResponse,
  bookings: BookingDetailResponse[],
): Map<string, DayAvailability> {
  const out = new Map<string, DayAvailability>();
  const today = startOfDay(new Date());
  const horizon = addDays(today, 365);
  const capacity = Number(shelter.capacity);
  const policy = Number(shelter.bookingPolicy);
  // Cancelled bookings stay in the response (so the user can see their history) but they don't
  // occupy capacity. The bbox-search handler on the API applies the same filter; without this
  // the date picker would keep a slot blocked even after the booking that owned it was cancelled.
  const activeBookings = bookings.filter(
    (b) => Number(b.status) !== BookingStatus.Cancelled,
  );

  for (let date = today; date <= horizon; date = addDays(date, 1)) {
    const key = format(date, 'yyyy-MM-dd');

    const checkoutBookings = activeBookings.filter((b) => {
      try {
        const bs = startOfDay(parseISO(b.startUtc));
        const be = startOfDay(parseISO(b.endUtc));
        return isBefore(bs, date) && !isBefore(be, date);
      } catch {
        return false;
      }
    });
    const checkinBookings = activeBookings.filter((b) => {
      try {
        const bs = startOfDay(parseISO(b.startUtc));
        const be = startOfDay(parseISO(b.endUtc));
        return !isBefore(date, bs) && isBefore(date, be);
      } catch {
        return false;
      }
    });

    const checkinGuests = checkinBookings.reduce((s, b) => s + Number(b.guests), 0);
    const checkoutGuests = checkoutBookings.reduce((s, b) => s + Number(b.guests), 0);
    const hasExclusiveCheckin = checkinBookings.some((b) => Number(b.type) === BookingType.Exclusive);
    const hasExclusiveCheckout = checkoutBookings.some(
      (b) => Number(b.type) === BookingType.Exclusive,
    );

    let checkinStatus: HalfStatus = 'available';
    let availableCapacityForCheckin = capacity;
    if (hasExclusiveCheckin) {
      checkinStatus = 'full';
      availableCapacityForCheckin = 0;
    } else if (policy === ShelterBookingPolicy.ExclusiveOnly && checkinGuests > 0) {
      checkinStatus = 'full';
      availableCapacityForCheckin = 0;
    } else if (checkinGuests >= capacity) {
      checkinStatus = 'full';
      availableCapacityForCheckin = 0;
    } else if (checkinGuests > 0) {
      checkinStatus = 'partial';
      availableCapacityForCheckin = capacity - checkinGuests;
    }

    let checkoutStatus: HalfStatus = 'available';
    if (hasExclusiveCheckout) checkoutStatus = 'full';
    else if (policy === ShelterBookingPolicy.ExclusiveOnly && checkoutGuests > 0)
      checkoutStatus = 'full';
    else if (checkoutGuests >= capacity) checkoutStatus = 'full';
    else if (checkoutGuests > 0) checkoutStatus = 'partial';

    out.set(key, {
      checkoutStatus,
      checkinStatus,
      checkoutGuests,
      checkinGuests,
      availableCapacityForCheckin,
    });
  }

  return out;
}

const STATUS_BORDER: Record<HalfStatus, string> = {
  available: '#22c55e', // green-500
  partial: '#f59e0b', // amber-500
  full: '#ef4444', // red-500
};

const STATUS_BG: Record<HalfStatus, string> = {
  available: '#dcfce7', // green-100
  partial: '#fef3c7', // amber-100
  full: '#fee2e2', // red-100
};

const SELECTION_COLOR = '#059669'; // primary-600
const SELECTION_BG = '#059669';
const PREVIEW_BG = '#34d399';

const WEEKDAYS = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

export function BookingDatePicker({
  shelter,
  bookings,
  selectedRange,
  onRangeChange,
  onCapacityChange,
}: BookingDatePickerProps) {
  const [currentMonth, setCurrentMonth] = useState(() => startOfMonth(new Date()));
  const [hoveredDate, setHoveredDate] = useState<Date | null>(null);
  const today = startOfDay(new Date());

  const availability = useMemo(
    () => calculateAvailability(shelter, bookings),
    [shelter, bookings],
  );

  const hasFullyBookedBetween = useCallback(
    (start: Date, end: Date): boolean => {
      for (let date = addDays(start, 1); isBefore(date, end); date = addDays(date, 1)) {
        const a = availability.get(format(date, 'yyyy-MM-dd'));
        if (a?.checkinStatus === 'full' && a?.checkoutStatus === 'full') return true;
      }
      return false;
    },
    [availability],
  );

  // Push effective max capacity for the selected range up to the parent so it can clamp the guest counter.
  useEffect(() => {
    if (!selectedRange?.from || !selectedRange?.to || !onCapacityChange) return;
    let minCapacity = Number(shelter.capacity);
    for (
      let date = selectedRange.from;
      isBefore(date, selectedRange.to);
      date = addDays(date, 1)
    ) {
      const a = availability.get(format(date, 'yyyy-MM-dd'));
      if (a) minCapacity = Math.min(minCapacity, a.availableCapacityForCheckin);
    }
    onCapacityChange(Math.max(0, minCapacity));
  }, [selectedRange, availability, shelter.capacity, onCapacityChange]);

  const leftMonth = currentMonth;
  const rightMonth = addMonths(currentMonth, 1);
  const leftDays = eachDayOfInterval({
    start: startOfMonth(leftMonth),
    end: endOfMonth(leftMonth),
  });
  const rightDays = eachDayOfInterval({
    start: startOfMonth(rightMonth),
    end: endOfMonth(rightMonth),
  });

  const previewRange = useMemo(() => {
    if (!selectedRange?.from || selectedRange?.to || !hoveredDate) return null;
    const start = selectedRange.from;
    const end = hoveredDate;
    return isBefore(end, start) ? { from: end, to: start } : { from: start, to: end };
  }, [selectedRange, hoveredDate]);

  const isPreviewValid = useMemo(() => {
    if (!previewRange) return true;
    return !hasFullyBookedBetween(previewRange.from, previewRange.to);
  }, [previewRange, hasFullyBookedBetween]);

  function handleDayClick(day: Date) {
    if (isBefore(day, today)) return;
    const a = availability.get(format(day, 'yyyy-MM-dd'));

    if (selectedRange?.from && selectedRange?.to) {
      if (a?.checkinStatus !== 'full') onRangeChange({ from: day, to: undefined });
      return;
    }
    if (selectedRange?.from && !selectedRange?.to) {
      const start = selectedRange.from;
      const [rangeStart, rangeEnd] = isBefore(day, start) ? [day, start] : [start, day];
      if (hasFullyBookedBetween(rangeStart, rangeEnd)) return;
      onRangeChange({ from: rangeStart, to: rangeEnd });
      return;
    }
    if (a?.checkinStatus !== 'full') onRangeChange({ from: day, to: undefined });
  }

  function isInRange(day: Date) {
    if (!selectedRange?.from || !selectedRange?.to) return false;
    try {
      return isWithinInterval(day, { start: selectedRange.from, end: selectedRange.to });
    } catch {
      return false;
    }
  }

  function isInPreview(day: Date) {
    if (!previewRange) return false;
    try {
      return isWithinInterval(day, { start: previewRange.from, end: previewRange.to });
    } catch {
      return false;
    }
  }

  const isRangeStart = (day: Date) =>
    selectedRange?.from ? isSameDay(day, selectedRange.from) : false;
  const isRangeEnd = (day: Date) =>
    selectedRange?.to ? isSameDay(day, selectedRange.to) : false;
  const isPreviewStart = (day: Date) =>
    previewRange ? isSameDay(day, previewRange.from) : false;
  const isPreviewEnd = (day: Date) =>
    previewRange ? isSameDay(day, previewRange.to) : false;

  function renderDayCell(day: Date, monthStart: Date) {
    const key = format(day, 'yyyy-MM-dd');
    const a = availability.get(key);
    const isPast = isBefore(day, today);
    const isCurrentMonth = isSameMonth(day, monthStart);
    const inRange = isInRange(day);
    const inPreview = isInPreview(day);
    const isStart = isRangeStart(day);
    const isEnd = isRangeEnd(day);
    const isPrevStart = isPreviewStart(day);
    const isPrevEnd = isPreviewEnd(day);

    const isSelectionStart =
      selectedRange?.from && !selectedRange?.to && isSameDay(day, selectedRange.from);
    const isDisabled = isPast || (a?.checkinStatus === 'full' && !selectedRange?.from);

    if (!isCurrentMonth) {
      return (
        <td key={key} className="p-0.5">
          <div className="h-12 w-12" />
        </td>
      );
    }

    const checkoutStatus = a?.checkoutStatus ?? 'available';
    const checkinStatus = a?.checkinStatus ?? 'available';
    const showCapacity =
      checkinStatus === 'partial' && a && !inRange && !isStart;
    const hasStatusDiagonal = checkoutStatus !== checkinStatus;
    const showSelectionHighlight = inRange || isSelectionStart;
    const showPreviewHighlight =
      inPreview && !inRange && !isSelectionStart && isPreviewValid;

    const highlightTopLeft =
      (inRange && !isStart) || (inPreview && !isPrevStart && isPreviewValid);
    const highlightBottomRight =
      (inRange && !isEnd) || isSelectionStart || (inPreview && !isPrevEnd && isPreviewValid);
    const highlightFull =
      (inRange && !isStart && !isEnd) ||
      (inPreview && !isPrevStart && !isPrevEnd && isPreviewValid);

    const overallBorder: HalfStatus =
      checkinStatus === 'full' || checkoutStatus === 'full'
        ? 'full'
        : checkinStatus === 'partial' || checkoutStatus === 'partial'
          ? 'partial'
          : 'available';

    return (
      <td key={key} className="p-0.5">
        <button
          type="button"
          onClick={() => handleDayClick(day)}
          onMouseEnter={() => {
            if (selectedRange?.from && !selectedRange?.to && !isPast) setHoveredDate(day);
          }}
          onMouseLeave={() => setHoveredDate(null)}
          disabled={isDisabled}
          aria-label={`${format(day, 'MMMM d, yyyy')}${
            showCapacity ? `, ${a.availableCapacityForCheckin} spots available` : ''
          }`}
          className={`relative h-12 w-12 overflow-hidden rounded-lg text-sm font-medium transition-all duration-150 ${
            isPast ? 'cursor-not-allowed opacity-40' : ''
          } ${isDisabled && !isPast ? 'cursor-not-allowed' : 'cursor-pointer'} ${
            inPreview && !isPreviewValid ? 'cursor-not-allowed' : ''
          }`}
        >
          {!hasStatusDiagonal && (
            <div
              className="absolute inset-0 rounded-lg border-2"
              style={{
                backgroundColor: STATUS_BG[checkinStatus],
                borderColor: STATUS_BORDER[checkinStatus],
              }}
            />
          )}
          {hasStatusDiagonal && (
            <>
              <div
                className="absolute inset-0"
                style={{
                  backgroundColor: STATUS_BG[checkoutStatus],
                  clipPath: 'polygon(0 0, 100% 0, 0 100%)',
                }}
              />
              <div
                className="absolute inset-0"
                style={{
                  backgroundColor: STATUS_BG[checkinStatus],
                  clipPath: 'polygon(100% 0, 100% 100%, 0 100%)',
                }}
              />
              <div
                className="absolute inset-0"
                style={{
                  background:
                    'linear-gradient(to top left, transparent calc(50% - 1px), #475569 calc(50% - 1px), #475569 calc(50% + 1px), transparent calc(50% + 1px))',
                }}
              />
              <div
                className="absolute inset-0 rounded-lg border-2"
                style={{ borderColor: STATUS_BORDER[overallBorder] }}
              />
            </>
          )}

          {(showSelectionHighlight || showPreviewHighlight) && (
            <>
              {highlightFull && (
                <div
                  className="absolute inset-0 rounded-lg"
                  style={{
                    backgroundColor: showPreviewHighlight ? PREVIEW_BG : SELECTION_BG,
                    opacity: showPreviewHighlight ? 0.5 : 0.6,
                  }}
                />
              )}
              {!highlightFull && (
                <>
                  {highlightTopLeft && (
                    <div
                      className="absolute inset-0"
                      style={{
                        backgroundColor: showPreviewHighlight ? PREVIEW_BG : SELECTION_BG,
                        opacity: showPreviewHighlight ? 0.5 : 0.6,
                        clipPath: 'polygon(0 0, 100% 0, 0 100%)',
                      }}
                    />
                  )}
                  {highlightBottomRight && (
                    <div
                      className="absolute inset-0"
                      style={{
                        backgroundColor: showPreviewHighlight ? PREVIEW_BG : SELECTION_BG,
                        opacity: showPreviewHighlight ? 0.5 : 0.6,
                        clipPath: 'polygon(100% 0, 100% 100%, 0 100%)',
                      }}
                    />
                  )}
                  {highlightTopLeft !== highlightBottomRight && (
                    <div
                      className="absolute inset-0 z-10"
                      style={{
                        background: `linear-gradient(to top left, transparent calc(50% - 1px), ${SELECTION_COLOR} calc(50% - 1px), ${SELECTION_COLOR} calc(50% + 1px), transparent calc(50% + 1px))`,
                      }}
                    />
                  )}
                </>
              )}
            </>
          )}

          {inPreview && !isPreviewValid && (
            <div
              className="absolute inset-0 rounded-lg"
              style={{ backgroundColor: '#ef4444', opacity: 0.3 }}
            />
          )}

          {(isStart || isEnd || isSelectionStart) && (
            <div
              className="absolute inset-0 rounded-lg"
              style={{ borderColor: SELECTION_COLOR, borderWidth: 3, borderStyle: 'solid' }}
            />
          )}

          <span
            className={`relative z-10 ${
              (highlightFull || (highlightTopLeft && highlightBottomRight)) &&
              !showPreviewHighlight
                ? 'font-bold text-white'
                : 'text-slate-900'
            }`}
          >
            {format(day, 'd')}
          </span>

          {showCapacity && (
            <span className="absolute bottom-0 right-0 z-20 rounded-tl bg-white/90 px-1 text-[9px] font-bold text-slate-700">
              {a.availableCapacityForCheckin} left
            </span>
          )}
        </button>
      </td>
    );
  }

  function renderMonth(monthStart: Date, days: Date[]) {
    const firstDayOfWeek = monthStart.getDay();
    const padded: (Date | null)[] = [...Array(firstDayOfWeek).fill(null), ...days];
    const weeks: (Date | null)[][] = [];
    for (let i = 0; i < padded.length; i += 7) weeks.push(padded.slice(i, i + 7));
    while (weeks[weeks.length - 1].length < 7) weeks[weeks.length - 1].push(null);

    return (
      <div className="flex-1">
        <h3 className="mb-4 text-center text-lg font-semibold text-slate-900">
          {format(monthStart, 'MMMM yyyy')}
        </h3>
        <table className="w-full border-collapse">
          <thead>
            <tr>
              {WEEKDAYS.map((d) => (
                <th
                  key={d}
                  className="p-1 text-center text-xs font-semibold text-slate-600"
                >
                  {d}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {weeks.map((week, weekIndex) => (
              <tr key={weekIndex}>
                {week.map((day, dayIndex) =>
                  day ? (
                    renderDayCell(day, monthStart)
                  ) : (
                    <td key={`empty-${dayIndex}`} className="p-0.5">
                      <div className="h-12 w-12" />
                    </td>
                  ),
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <button
          type="button"
          onClick={() => setCurrentMonth(subMonths(currentMonth, 1))}
          disabled={isSameMonth(currentMonth, today)}
          aria-label="Previous month"
          className="rounded-lg p-2 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40"
        >
          <ChevronLeft className="h-5 w-5 text-slate-600" />
        </button>
        <button
          type="button"
          onClick={() => setCurrentMonth(addMonths(currentMonth, 1))}
          aria-label="Next month"
          className="rounded-lg p-2 transition-colors hover:bg-slate-100"
        >
          <ChevronRight className="h-5 w-5 text-slate-600" />
        </button>
      </div>
      <div className="flex gap-8">
        {renderMonth(leftMonth, leftDays)}
        {renderMonth(rightMonth, rightDays)}
      </div>

      {selectedRange?.from && !selectedRange?.to && (
        <div className="mt-4 rounded-lg border border-primary-200 bg-primary-50 p-2 text-sm text-primary-800">
          <strong>Check-in:</strong> {format(selectedRange.from, 'MMMM d, yyyy')} — Now
          select your check-out date
        </div>
      )}

      <div className="mt-6 border-t border-slate-200 pt-4">
        <p className="mb-3 text-xs font-semibold text-slate-700">Legend:</p>
        <div className="flex flex-wrap gap-4 text-xs">
          <LegendSwatch bg={STATUS_BG.available} border={STATUS_BORDER.available} label="Available" />
          <LegendSwatch bg={STATUS_BG.partial} border={STATUS_BORDER.partial} label="Partial (shared)" />
          <LegendSwatch bg={STATUS_BG.full} border={STATUS_BORDER.full} label="Fully booked" />
          <LegendSwatch bg={SELECTION_BG} border={SELECTION_COLOR} label="Your selection" />
        </div>
      </div>
    </div>
  );
}

function LegendSwatch({
  bg,
  border,
  label,
}: {
  bg: string;
  border: string;
  label: string;
}) {
  return (
    <div className="flex items-center gap-2">
      <div className="h-6 w-6 rounded border-2" style={{ backgroundColor: bg, borderColor: border }} />
      <span className="text-slate-600">{label}</span>
    </div>
  );
}
