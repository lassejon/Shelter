import { useCallback, useRef, useState } from 'react';
import ShelterMap, { type FlyTo } from '@/features/map/components/ShelterMap';
import { MapHeader } from '@/features/search/components/MapHeader';
import { ShelterCard } from '@/features/shelters/components/ShelterCard';
import type { SearchShelterResponse } from '@/features/map/models/dto';

interface SelectedShelter {
  shelter: SearchShelterResponse;
  x: number;
  y: number;
}

export default function HomePage() {
  const flyToRef = useRef<FlyTo | undefined>(undefined);
  const [mapCenter, setMapCenter] = useState<{ lat: number; lng: number } | undefined>(undefined);
  const [selected, setSelected] = useState<SelectedShelter | null>(null);

  const handleMapReady = useCallback((flyTo: FlyTo) => {
    flyToRef.current = flyTo;
  }, []);

  const handleSelectLocation = useCallback<FlyTo>((lat, lng, zoom) => {
    flyToRef.current?.(lat, lng, zoom);
  }, []);

  return (
    <div className="relative h-screen w-screen overflow-hidden">
      <ShelterMap
        onMapReady={handleMapReady}
        onCenterChange={setMapCenter}
        onShelterClick={(data) => setSelected(data)}
      />
      <MapHeader onSelectLocation={handleSelectLocation} mapCenter={mapCenter} />
      {selected && (
        <ShelterCard
          shelter={selected.shelter}
          position={{ x: selected.x, y: selected.y }}
          onClose={() => setSelected(null)}
        />
      )}
    </div>
  );
}
