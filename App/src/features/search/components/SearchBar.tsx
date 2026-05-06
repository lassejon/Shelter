import { useCallback, useEffect, useRef, useState } from 'react';
import { Loader2, MapPin, Search } from 'lucide-react';
import { useMapFilterStore } from '@/shared/stores/map-filter.store';
import { usePlacePredictions } from '@/features/search/hooks/usePlacePredictions';
import {
  getPlaceDetails,
  initializePlacesService,
  type PlacePrediction,
} from '@/features/search/services/places';

interface SearchBarProps {
  onSelectLocation?: (lat: number, lng: number, zoom?: number) => void;
  mapCenter?: { lat: number; lng: number };
}

export function SearchBar({ onSelectLocation, mapCenter }: SearchBarProps) {
  const searchQuery = useMapFilterStore((s) => s.searchQuery);
  const setSearchQuery = useMapFilterStore((s) => s.setSearchQuery);

  const [open, setOpen] = useState(false);
  const [highlighted, setHighlighted] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const { results, isLoading } = usePlacePredictions(searchQuery, mapCenter, 300);

  useEffect(() => {
    const timer = setTimeout(() => initializePlacesService(), 500);
    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    function onClick(event: MouseEvent) {
      const target = event.target as Node;
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(target) &&
        inputRef.current &&
        !inputRef.current.contains(target)
      ) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, []);

  const shouldShow = open && searchQuery.trim().length >= 2;

  const select = useCallback(
    async (item: PlacePrediction) => {
      setSearchQuery(item.primaryText);
      setOpen(false);
      const details = await getPlaceDetails(item.placeId);
      if (details) onSelectLocation?.(details.latitude, details.longitude, 14);
    },
    [onSelectLocation, setSearchQuery],
  );

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (!shouldShow) return;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlighted((p) => (p < results.length - 1 ? p + 1 : p));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlighted((p) => (p > 0 ? p - 1 : -1));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (highlighted >= 0 && highlighted < results.length) select(results[highlighted]);
    } else if (e.key === 'Escape') {
      e.preventDefault();
      setOpen(false);
      inputRef.current?.blur();
    }
  };

  return (
    <div className="relative w-96">
      <Search
        className="pointer-events-none absolute left-3 top-1/2 z-10 -translate-y-1/2 text-slate-400"
        size={18}
      />
      <input
        ref={inputRef}
        type="text"
        value={searchQuery}
        onChange={(e) => {
          setSearchQuery(e.target.value);
          setOpen(true);
          setHighlighted(-1);
        }}
        onFocus={() => {
          if (searchQuery.trim().length >= 2) setOpen(true);
        }}
        onKeyDown={onKeyDown}
        placeholder="Search shelters by name, location..."
        className="w-full rounded-full border border-slate-300 py-2 pl-10 pr-4 text-sm transition-shadow focus:outline-none focus:ring-2 focus:ring-primary-500"
        role="combobox"
        aria-expanded={shouldShow}
        aria-controls="search-dropdown"
        aria-activedescendant={highlighted >= 0 ? `search-item-${highlighted}` : undefined}
      />

      {shouldShow && (
        <div
          ref={dropdownRef}
          id="search-dropdown"
          role="listbox"
          className="absolute left-0 right-0 top-full z-50 mt-2 max-h-96 overflow-y-auto rounded-lg border border-slate-200 bg-white shadow-lg"
        >
          {results.length > 0 ? (
            <div>
              <div className="flex items-center justify-between border-b border-slate-100 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                <span>Places</span>
                {isLoading && <Loader2 className="animate-spin" size={14} />}
              </div>
              {results.map((item, index) => {
                const isHighlighted = highlighted === index;
                return (
                  <button
                    key={item.placeId}
                    id={`search-item-${index}`}
                    type="button"
                    role="option"
                    aria-selected={isHighlighted}
                    onClick={() => select(item)}
                    onMouseEnter={() => setHighlighted(index)}
                    className={`flex w-full items-start gap-3 px-4 py-3 text-left transition-colors ${
                      isHighlighted ? 'bg-primary-50' : 'hover:bg-slate-50'
                    }`}
                  >
                    <MapPin className="mt-0.5 shrink-0 text-slate-600" size={18} />
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-sm font-medium text-slate-900">
                        {item.primaryText}
                      </div>
                      {item.secondaryText && (
                        <div className="truncate text-xs text-slate-500">{item.secondaryText}</div>
                      )}
                    </div>
                  </button>
                );
              })}
            </div>
          ) : isLoading ? (
            <div className="flex items-center justify-center gap-2 px-4 py-8 text-sm text-slate-500">
              <Loader2 className="animate-spin" size={16} />
              <span>Searching…</span>
            </div>
          ) : (
            <div className="px-4 py-8 text-center text-sm text-slate-500">
              No results for &ldquo;{searchQuery}&rdquo;
            </div>
          )}
        </div>
      )}
    </div>
  );
}
