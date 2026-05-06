import { Link, useNavigate } from 'react-router';
import { Button } from '@/shared/ui/Button';
import { LoginDropdown } from '@/features/auth/components/LoginDropdown';
import { useHasRole } from '@/features/auth/hooks/useHasRole';
import { SearchBar } from './SearchBar';
import { GuestSelector } from './GuestSelector';
import { DatePicker } from './DatePicker';
import { FilterButton } from './FilterButton';

interface MapHeaderProps {
  onSelectLocation?: (lat: number, lng: number, zoom?: number) => void;
  mapCenter?: { lat: number; lng: number };
}

export function MapHeader({ onSelectLocation, mapCenter }: MapHeaderProps) {
  const navigate = useNavigate();
  const canCreateShelter = useHasRole('ShelterOwner');

  return (
    <header className="fixed left-0 right-0 top-0 z-40 bg-white/95 shadow-lg backdrop-blur-sm">
      <div className="flex h-20 items-center justify-between gap-4 px-10">
        <Link to="/" className="text-2xl font-bold text-primary-600">
          🏕️ Shelter
        </Link>

        <div className="flex items-center gap-4">
          <SearchBar onSelectLocation={onSelectLocation} mapCenter={mapCenter} />
          <GuestSelector />
          <DatePicker />
          <FilterButton />
        </div>

        <div className="flex items-center gap-3">
          {canCreateShelter && (
            <Button variant="primary" onClick={() => navigate('/shelters/create')}>
              Create Shelter
            </Button>
          )}
          <LoginDropdown />
        </div>
      </div>
    </header>
  );
}
