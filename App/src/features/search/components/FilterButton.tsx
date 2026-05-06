import { useState } from 'react';
import { SlidersHorizontal } from 'lucide-react';
import { FilterModal } from './FilterModal';

export function FilterButton() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex items-center gap-2 rounded-lg border border-slate-300 px-4 py-2 transition-colors hover:bg-slate-50"
      >
        <SlidersHorizontal size={18} className="text-slate-600" />
        <span className="text-sm font-medium text-slate-700">Filters</span>
      </button>
      <FilterModal isOpen={open} onClose={() => setOpen(false)} />
    </>
  );
}
