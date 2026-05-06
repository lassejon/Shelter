import { useState } from 'react';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { Minus, Plus, Users } from 'lucide-react';
import { useMapFilterStore } from '@/shared/stores/map-filter.store';

const MIN = 1;
const MAX = 50;

export function GuestSelector() {
  const [open, setOpen] = useState(false);
  const guests = useMapFilterStore((s) => s.guests);
  const setGuests = useMapFilterStore((s) => s.setGuests);

  const current = guests ?? MIN;
  const buttonText =
    guests === null ? 'Add guests' : guests === 1 ? '1 guest' : `${guests} guests`;

  return (
    <DropdownMenu.Root open={open} onOpenChange={setOpen}>
      <DropdownMenu.Trigger asChild>
        <button
          type="button"
          className="flex items-center gap-2 rounded-lg border border-slate-300 px-4 py-2 transition-colors hover:bg-slate-50"
        >
          <Users size={18} className="text-slate-600" />
          <span className="text-sm font-medium text-slate-700">{buttonText}</span>
        </button>
      </DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content
          align="start"
          sideOffset={8}
          className="z-50 min-w-[200px] rounded-lg border border-slate-200 bg-white p-4 shadow-lg"
        >
          <div className="mb-4 text-sm font-medium text-slate-700">Guests</div>
          <div className="mb-4 flex items-center justify-between">
            <button
              type="button"
              disabled={current <= MIN}
              onClick={() => setGuests(Math.max(MIN, current - 1))}
              aria-label="Decrease guests"
              className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 transition-colors hover:bg-slate-200 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:opacity-50"
            >
              <Minus size={18} className="text-slate-700" />
            </button>
            <span className="min-w-[3ch] text-center text-2xl font-semibold text-slate-900">
              {current}
            </span>
            <button
              type="button"
              disabled={current >= MAX}
              onClick={() => setGuests(Math.min(MAX, current + 1))}
              aria-label="Increase guests"
              className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 transition-colors hover:bg-slate-200 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:opacity-50"
            >
              <Plus size={18} className="text-slate-700" />
            </button>
          </div>
          <div className="flex items-center justify-between gap-2 border-t border-slate-200 pt-3">
            <button
              type="button"
              onClick={() => {
                setGuests(null);
                setOpen(false);
              }}
              className="rounded-lg px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 hover:text-slate-900"
            >
              Clear
            </button>
            <button
              type="button"
              onClick={() => setOpen(false)}
              className="rounded-lg bg-primary-600 px-6 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700"
            >
              Done
            </button>
          </div>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}
