import { useCallback, useRef, useState } from 'react';
import type { BoundingBox } from '@/features/map/models/dto';

interface ViewportState {
  bbox: BoundingBox | null;
  zoom: number;
  center: { lat: number; lng: number };
}

const DEBOUNCE_MS = 250;
const BBOX_PADDING_RATIO = 0.1;

export function useMapViewport() {
  const [viewport, setViewport] = useState<ViewportState>({
    bbox: null,
    zoom: 11,
    center: { lat: 55.6761, lng: 12.5683 },
  });

  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const updateViewport = useCallback((map: google.maps.Map) => {
    if (debounceRef.current) clearTimeout(debounceRef.current);

    debounceRef.current = setTimeout(() => {
      const bounds = map.getBounds();
      const zoom = map.getZoom();
      const center = map.getCenter();
      if (!bounds || zoom === undefined || !center) return;

      const ne = bounds.getNorthEast();
      const sw = bounds.getSouthWest();
      const latPad = (ne.lat() - sw.lat()) * BBOX_PADDING_RATIO;
      const lngPad = (ne.lng() - sw.lng()) * BBOX_PADDING_RATIO;

      setViewport({
        bbox: {
          minLatitude: sw.lat() - latPad,
          maxLatitude: ne.lat() + latPad,
          minLongitude: sw.lng() - lngPad,
          maxLongitude: ne.lng() + lngPad,
        },
        zoom,
        center: { lat: center.lat(), lng: center.lng() },
      });
    }, DEBOUNCE_MS);
  }, []);

  const cleanup = useCallback(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
  }, []);

  return { ...viewport, updateViewport, cleanup };
}
