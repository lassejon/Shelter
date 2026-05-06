import { useCallback, useRef, useState } from 'react';
import { useNavigate } from 'react-router';
import ShelterMap, { type FlyTo } from '@/features/map/components/ShelterMap';
import { MapHeader } from '@/features/search/components/MapHeader';

export default function HomePage() {
  const navigate = useNavigate();
  const flyToRef = useRef<FlyTo | undefined>(undefined);
  const [mapCenter, setMapCenter] = useState<{ lat: number; lng: number } | undefined>(undefined);

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
        onShelterClick={({ shelter }) => navigate(`/shelters/${shelter.id}`)}
      />
      <MapHeader onSelectLocation={handleSelectLocation} mapCenter={mapCenter} />
    </div>
  );
}
