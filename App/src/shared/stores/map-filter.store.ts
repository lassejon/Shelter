import { create } from 'zustand';

interface FilterState {
  minCapacity: number | null;
  maxCapacity: number | null;
  minRating: number | null;
}

interface MapFilterState {
  searchQuery: string;
  filters: FilterState;
  dates: { start: Date | null; end: Date | null };
  guests: number | null;
}

interface MapFilterActions {
  setSearchQuery: (query: string) => void;
  setFilters: (filters: Partial<FilterState>) => void;
  setDates: (dates: { start: Date | null; end: Date | null }) => void;
  setGuests: (guests: number | null) => void;
  clearAll: () => void;
}

type MapFilterStore = MapFilterState & MapFilterActions;

const defaultFilters: FilterState = {
  minCapacity: null,
  maxCapacity: null,
  minRating: null,
};

const defaultState: MapFilterState = {
  searchQuery: '',
  filters: defaultFilters,
  dates: { start: null, end: null },
  guests: null,
};

export const useMapFilterStore = create<MapFilterStore>((set) => ({
  ...defaultState,

  setSearchQuery: (searchQuery) => set({ searchQuery }),
  setFilters: (newFilters) =>
    set((state) => ({ filters: { ...state.filters, ...newFilters } })),
  setDates: (dates) => set({ dates }),
  setGuests: (guests) => set({ guests }),
  clearAll: () => set({ ...defaultState }),
}));

export type { FilterState, MapFilterState };
