import { useEffect } from 'react';
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps';
import { env } from '@/shared/config/env';
import { useBboxShelters } from '@/features/map/hooks/useBboxShelters';
import { useMapViewport } from '@/features/map/hooks/useMapViewport';
import { useMapFilterStore } from '@/features/map/stores/map-filter.store';
import type { SearchShelterResponse } from '@/features/map/models/dto';
import { DeckGLOverlay } from './DeckGLOverlay';

interface ShelterMapProps {
  initialCenter?: { lat: number; lng: number };
  initialZoom?: number;
  onShelterClick?: (data: { shelter: SearchShelterResponse; x: number; y: number }) => void;
}

const DEFAULT_CENTER = { lat: 55.6761, lng: 12.5683 };
const DEFAULT_ZOOM = 11;

interface ViewportSyncProps {
  onViewportChange: (map: google.maps.Map) => void;
}

function ViewportSync({ onViewportChange }: ViewportSyncProps) {
  const map = useMap();

  useEffect(() => {
    if (!map) return;
    const handleIdle = () => onViewportChange(map);
    const listener = map.addListener('idle', handleIdle);
    if (map.getBounds()) handleIdle();
    return () => google.maps.event.removeListener(listener);
  }, [map, onViewportChange]);

  return null;
}

export default function ShelterMap({
  initialCenter = DEFAULT_CENTER,
  initialZoom = DEFAULT_ZOOM,
  onShelterClick,
}: ShelterMapProps) {
  const { bbox, updateViewport, cleanup } = useMapViewport();
  const filters = useMapFilterStore((state) => state.filters);

  useEffect(() => () => cleanup(), [cleanup]);

  const { data: shelters = [] } = useBboxShelters({
    bbox,
    filters: {
      minRating: filters.minRating,
      minCapacity: filters.minCapacity,
      maxCapacity: filters.maxCapacity,
    },
  });

  return (
    <APIProvider apiKey={env.googleMapsApiKey} libraries={['places']}>
      <Map
        defaultCenter={initialCenter}
        defaultZoom={initialZoom}
        mapId={env.googleMapId}
        gestureHandling="greedy"
        disableDefaultUI
        minZoom={2}
        maxZoom={20}
        restriction={{
          latLngBounds: { north: 85, south: -85, west: -180, east: 180 },
          strictBounds: true,
        }}
        mapTypeControl={false}
        streetViewControl={false}
        fullscreenControl={false}
        style={{ width: '100%', height: '100%' }}
      >
        <ViewportSync onViewportChange={updateViewport} />
        <DeckGLOverlay shelters={shelters} onShelterClick={onShelterClick} />
      </Map>
    </APIProvider>
  );
}
