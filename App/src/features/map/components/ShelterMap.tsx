import { useCallback, useEffect } from 'react';
import { APIProvider, Map, useMap } from '@vis.gl/react-google-maps';
import { env } from '@/shared/config/env';
import { useBboxShelters } from '@/features/map/hooks/useBboxShelters';
import { useMapViewport } from '@/features/map/hooks/useMapViewport';
import { useMapFilterStore } from '@/shared/stores/map-filter.store';
import type { SearchShelterResponse } from '@/features/map/models/dto';
import { DeckGLOverlay } from './DeckGLOverlay';

export type FlyTo = (lat: number, lng: number, zoom?: number) => void;

interface ShelterMapProps {
  initialCenter?: { lat: number; lng: number };
  initialZoom?: number;
  onMapReady?: (flyTo: FlyTo) => void;
  onCenterChange?: (center: { lat: number; lng: number }) => void;
  onShelterClick?: (data: { shelter: SearchShelterResponse; x: number; y: number }) => void;
}

const DEFAULT_CENTER = { lat: 55.6761, lng: 12.5683 };
const DEFAULT_ZOOM = 11;

interface MapControllerProps {
  onMapReady?: (flyTo: FlyTo) => void;
  onCenterChange?: (center: { lat: number; lng: number }) => void;
  onViewportChange: (map: google.maps.Map) => void;
}

function MapController({ onMapReady, onCenterChange, onViewportChange }: MapControllerProps) {
  const map = useMap();

  const flyTo = useCallback<FlyTo>(
    (lat, lng, zoom) => {
      if (!map) return;
      if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;
      map.panTo({ lat, lng });
      if (zoom !== undefined && Number.isFinite(zoom)) map.setZoom(zoom);
    },
    [map],
  );

  useEffect(() => {
    if (map && onMapReady) onMapReady(flyTo);
  }, [map, onMapReady, flyTo]);

  useEffect(() => {
    if (!map) return;
    const handleIdle = () => {
      onViewportChange(map);
      if (onCenterChange) {
        const center = map.getCenter();
        if (center) onCenterChange({ lat: center.lat(), lng: center.lng() });
      }
    };
    const listener = map.addListener('idle', handleIdle);
    if (map.getBounds()) handleIdle();
    return () => google.maps.event.removeListener(listener);
  }, [map, onCenterChange, onViewportChange]);

  return null;
}

export default function ShelterMap({
  initialCenter = DEFAULT_CENTER,
  initialZoom = DEFAULT_ZOOM,
  onMapReady,
  onCenterChange,
  onShelterClick,
}: ShelterMapProps) {
  const { bbox, updateViewport, cleanup } = useMapViewport();
  const filters = useMapFilterStore((state) => state.filters);
  const guests = useMapFilterStore((state) => state.guests);
  const dates = useMapFilterStore((state) => state.dates);

  useEffect(() => () => cleanup(), [cleanup]);

  const { data: shelters = [] } = useBboxShelters({
    bbox,
    filters: {
      minRating: filters.minRating,
      minCapacity: filters.minCapacity,
      maxCapacity: filters.maxCapacity,
    },
    guests,
    dates,
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
        <MapController
          onMapReady={onMapReady}
          onCenterChange={onCenterChange}
          onViewportChange={updateViewport}
        />
        <DeckGLOverlay shelters={shelters} onShelterClick={onShelterClick} />
      </Map>
    </APIProvider>
  );
}
