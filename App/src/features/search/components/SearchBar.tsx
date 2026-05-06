import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Home, Loader2, MapPin, Search } from 'lucide-react';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { useMapFilterStore } from '@/shared/stores/map-filter.store';
import { usePlacePredictions } from '@/features/search/hooks/usePlacePredictions';
import { useShelterSearch } from '@/features/search/hooks/useShelterSearch';
import {
  getPlaceDetails,
  initializePlacesService,
  type PlacePrediction,
} from '@/features/search/services/places';
import type { SearchShelterResponse } from '@/features/map/models/dto';

interface SearchBarProps {
  onSelectLocation?: (lat: number, lng: number, zoom?: number) => void;
  mapCenter?: { lat: number; lng: number };
}

type SearchItem =
  | { kind: 'shelter'; shelter: SearchShelterResponse }
  | { kind: 'place'; place: PlacePrediction };

export function SearchBar({ onSelectLocation, mapCenter }: SearchBarProps) {
  const searchQuery = useMapFilterStore((s) => s.searchQuery);
  const setSearchQuery = useMapFilterStore((s) => s.setSearchQuery);

  const debouncedQuery = useDebouncedValue(searchQuery, 300);

  const [open, setOpen] = useState(false);
  const [highlighted, setHighlighted] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const { results: shelterResults, isLoading: isLoadingShelters } =
    useShelterSearch(debouncedQuery);
  const { results: placeResults, isLoading: isLoadingPlaces } =
    usePlacePredictions(debouncedQuery, mapCenter);

  const items = useMemo<SearchItem[]>(
    () => [
      ...shelterResults.map<SearchItem>((shelter) => ({ kind: 'shelter', shelter })),
      ...placeResults.map<SearchItem>((place) => ({ kind: 'place', place })),
    ],
    [shelterResults, placeResults],
  );

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
  const isLoading = isLoadingShelters || isLoadingPlaces;
  const hasResults = items.length > 0;

  const select = useCallback(
    async (item: SearchItem) => {
      if (item.kind === 'shelter') {
        setSearchQuery(item.shelter.name);
        setOpen(false);
        onSelectLocation?.(
          Number(item.shelter.latitude),
          Number(item.shelter.longitude),
          15,
        );
        return;
      }
      setSearchQuery(item.place.primaryText);
      setOpen(false);
      const details = await getPlaceDetails(item.place.placeId);
      if (details) onSelectLocation?.(details.latitude, details.longitude, 14);
    },
    [onSelectLocation, setSearchQuery],
  );

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (!shouldShow) return;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlighted((p) => (p < items.length - 1 ? p + 1 : p));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlighted((p) => (p > 0 ? p - 1 : -1));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (highlighted >= 0 && highlighted < items.length) select(items[highlighted]);
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
          {hasResults ? (
            <>
              {shelterResults.length > 0 && (
                <SectionHeader label="Shelters" loading={isLoadingShelters} />
              )}
              {shelterResults.map((shelter, index) => {
                const globalIndex = index;
                const isHighlighted = highlighted === globalIndex;
                return (
                  <SearchRow
                    key={`shelter-${shelter.id}`}
                    id={`search-item-${globalIndex}`}
                    icon={<Home className="mt-0.5 shrink-0 text-primary-600" size={18} />}
                    primary={shelter.name}
                    secondary={shelter.description ?? undefined}
                    highlighted={isHighlighted}
                    onMouseEnter={() => setHighlighted(globalIndex)}
                    onClick={() => select({ kind: 'shelter', shelter })}
                  />
                );
              })}
              {placeResults.length > 0 && (
                <SectionHeader label="Places" loading={isLoadingPlaces} />
              )}
              {placeResults.map((place, index) => {
                const globalIndex = shelterResults.length + index;
                const isHighlighted = highlighted === globalIndex;
                return (
                  <SearchRow
                    key={`place-${place.placeId}`}
                    id={`search-item-${globalIndex}`}
                    icon={<MapPin className="mt-0.5 shrink-0 text-slate-600" size={18} />}
                    primary={place.primaryText}
                    secondary={place.secondaryText}
                    highlighted={isHighlighted}
                    onMouseEnter={() => setHighlighted(globalIndex)}
                    onClick={() => select({ kind: 'place', place })}
                  />
                );
              })}
            </>
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

function SectionHeader({ label, loading }: { label: string; loading: boolean }) {
  return (
    <div className="flex items-center justify-between border-b border-slate-100 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
      <span>{label}</span>
      {loading && <Loader2 className="animate-spin" size={14} />}
    </div>
  );
}

interface SearchRowProps {
  id: string;
  icon: React.ReactNode;
  primary: string;
  secondary?: string | null;
  highlighted: boolean;
  onMouseEnter: () => void;
  onClick: () => void;
}

function SearchRow({ id, icon, primary, secondary, highlighted, onMouseEnter, onClick }: SearchRowProps) {
  return (
    <button
      id={id}
      type="button"
      role="option"
      aria-selected={highlighted}
      onClick={onClick}
      onMouseEnter={onMouseEnter}
      className={`flex w-full items-start gap-3 px-4 py-3 text-left transition-colors ${
        highlighted ? 'bg-primary-50' : 'hover:bg-slate-50'
      }`}
    >
      {icon}
      <div className="min-w-0 flex-1">
        <div className="truncate text-sm font-medium text-slate-900">{primary}</div>
        {secondary && (
          <div className="truncate text-xs text-slate-500">{secondary}</div>
        )}
      </div>
    </button>
  );
}
