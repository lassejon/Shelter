import { useState } from 'react';
import * as Popover from '@radix-ui/react-popover';
import { Calendar as CalendarIcon } from 'lucide-react';
import { DayPicker, type DateRange } from 'react-day-picker';
import { format } from 'date-fns';
import { useMapFilterStore } from '@/shared/stores/map-filter.store';
import 'react-day-picker/style.css';

export function DatePicker() {
  const [open, setOpen] = useState(false);
  const dates = useMapFilterStore((s) => s.dates);
  const setDates = useMapFilterStore((s) => s.setDates);

  const selected: DateRange | undefined = dates.start
    ? { from: dates.start, to: dates.end ?? undefined }
    : undefined;

  const buttonText =
    dates.start && dates.end
      ? `${format(dates.start, 'MMM d')} – ${format(dates.end, 'MMM d')}`
      : dates.start
        ? format(dates.start, 'MMM d')
        : 'Dates';

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button
          type="button"
          className="flex items-center gap-2 rounded-lg border border-slate-300 px-4 py-2 transition-colors hover:bg-slate-50"
        >
          <CalendarIcon size={18} className="text-slate-600" />
          <span className="text-sm font-medium text-slate-700">{buttonText}</span>
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={8}
          className="z-50 rounded-lg border border-slate-200 bg-white p-4 shadow-lg"
        >
          <DayPicker
            mode="range"
            selected={selected}
            onSelect={(range) =>
              setDates({ start: range?.from ?? null, end: range?.to ?? null })
            }
            numberOfMonths={2}
            className="text-sm"
          />
          <div className="mt-3 flex items-center justify-between gap-2 border-t border-slate-200 pt-3">
            <button
              type="button"
              onClick={() => {
                setDates({ start: null, end: null });
                setOpen(false);
              }}
              className="rounded-lg px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 hover:text-slate-900"
            >
              Clear dates
            </button>
            <button
              type="button"
              onClick={() => setOpen(false)}
              className="rounded-lg bg-primary-600 px-6 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700"
            >
              Done
            </button>
          </div>
          <Popover.Arrow className="fill-white" />
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
