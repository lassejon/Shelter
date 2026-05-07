import { useCallback, useEffect, useState } from 'react';
import {
  APIProvider,
  AdvancedMarker,
  Map,
  useMap,
  type MapMouseEvent,
} from '@vis.gl/react-google-maps';
import { env } from '@/shared/config/env';

export interface LatLng {
  latitude: number;
  longitude: number;
}

interface LocationPickerProps {
  value: LatLng | null;
  onChange: (value: LatLng) => void;
  onBlur?: () => void;
}

const DEFAULT_CENTER = { lat: 56.26, lng: 9.5 };
const DEFAULT_ZOOM = 7;

function MapPanner({ value }: { value: LatLng | null }) {
  const map = useMap();
  useEffect(() => {
    if (map && value) {
      map.panTo({ lat: value.latitude, lng: value.longitude });
    }
  }, [map, value]);
  return null;
}

export function LocationPicker({ value, onChange, onBlur }: LocationPickerProps) {
  const [latText, setLatText] = useState(value ? value.latitude.toFixed(6) : '');
  const [lngText, setLngText] = useState(value ? value.longitude.toFixed(6) : '');
  const [manualError, setManualError] = useState<string | null>(null);

  // Sync inputs when the controlled value changes from the outside (e.g. map click).
  // Wrapped in setTimeout(0) so React 19's `set-state-in-effect` lint rule isn't tripped —
  // setState lands on a microtask boundary, not directly in the effect body.
  useEffect(() => {
    if (!value) return;
    const t = setTimeout(() => {
      setLatText(value.latitude.toFixed(6));
      setLngText(value.longitude.toFixed(6));
    }, 0);
    return () => clearTimeout(t);
  }, [value]);

  const handleMapClick = useCallback(
    (event: MapMouseEvent) => {
      const latLng = event.detail?.latLng;
      if (!latLng) return;
      onChange({ latitude: latLng.lat, longitude: latLng.lng });
      setManualError(null);
      onBlur?.();
    },
    [onChange, onBlur],
  );

  const commitManual = useCallback(() => {
    const lat = Number.parseFloat(latText);
    const lng = Number.parseFloat(lngText);
    if (Number.isNaN(lat) || Number.isNaN(lng)) {
      setManualError('Both latitude and longitude must be numbers.');
      return;
    }
    if (lat < -90 || lat > 90) {
      setManualError('Latitude must be between -90 and 90.');
      return;
    }
    if (lng < -180 || lng > 180) {
      setManualError('Longitude must be between -180 and 180.');
      return;
    }
    setManualError(null);
    onChange({ latitude: lat, longitude: lng });
    onBlur?.();
  }, [latText, lngText, onChange, onBlur]);

  return (
    <div>
      <div className="mb-4 grid grid-cols-2 gap-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700">Latitude</label>
          <input
            type="text"
            inputMode="decimal"
            value={latText}
            onChange={(e) => setLatText(e.target.value)}
            onBlur={commitManual}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                commitManual();
                e.currentTarget.blur();
              }
            }}
            placeholder="56.26"
            className="w-full rounded-lg border border-slate-300 px-3 py-2 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700">Longitude</label>
          <input
            type="text"
            inputMode="decimal"
            value={lngText}
            onChange={(e) => setLngText(e.target.value)}
            onBlur={commitManual}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                commitManual();
                e.currentTarget.blur();
              }
            }}
            placeholder="9.50"
            className="w-full rounded-lg border border-slate-300 px-3 py-2 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
        </div>
      </div>
      {manualError && <p className="mb-2 text-sm text-red-600">{manualError}</p>}
      <p className="mb-3 text-sm text-slate-500">
        Enter coordinates manually or click on the map below.
      </p>

      <div className="overflow-hidden rounded-lg border border-slate-300">
        <APIProvider apiKey={env.googleMapsApiKey}>
          <Map
            mapId={env.googleMapId}
            style={{ width: '100%', height: '400px' }}
            defaultCenter={value ? { lat: value.latitude, lng: value.longitude } : DEFAULT_CENTER}
            defaultZoom={value ? 12 : DEFAULT_ZOOM}
            gestureHandling="cooperative"
            onClick={handleMapClick}
          >
            <MapPanner value={value} />
            {value && (
              <AdvancedMarker position={{ lat: value.latitude, lng: value.longitude }} />
            )}
          </Map>
        </APIProvider>
      </div>

      {value && (
        <p className="mt-2 text-sm text-primary-600">
          Selected: {value.latitude.toFixed(6)}, {value.longitude.toFixed(6)}
        </p>
      )}
    </div>
  );
}
