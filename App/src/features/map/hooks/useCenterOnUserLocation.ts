import { useEffect } from 'react';
import type { FlyTo } from '@/features/map/components/ShelterMap';

const USER_LOCATION_ZOOM = 14;

// Module-level so that navigating away from the home page and returning doesn't
// re-prompt for permission or yank the user's pan/zoom back to their physical
// location. Ask once per tab session; user can refresh to reset.
let hasCenteredOnUser = false;

/**
 * On first map-ready render in this tab session, asks the browser for the
 * user's location and pans the map there with a fixed zoom. Silent on
 * permission denial / timeout / unsupported browser — the default centre
 * stays in place. Will not re-prompt on subsequent mounts.
 *
 * Pass `null` until the map is ready (so an early geolocate doesn't no-op
 * against an unmounted map and burn the once-per-session slot).
 */
export function useCenterOnUserLocation(flyTo: FlyTo | null) {
  useEffect(() => {
    if (!flyTo || hasCenteredOnUser) return;
    if (typeof navigator === 'undefined' || !navigator.geolocation) return;

    // Claim the slot before the async resolves so a fast unmount/remount
    // can't trigger a second prompt.
    hasCenteredOnUser = true;
    navigator.geolocation.getCurrentPosition(
      (position) => {
        flyTo(position.coords.latitude, position.coords.longitude, USER_LOCATION_ZOOM);
      },
      () => {
        // silent: permission denied, timeout, or position unavailable —
        // keep the default Copenhagen centre.
      },
      { enableHighAccuracy: false, timeout: 5000, maximumAge: 5 * 60_000 },
    );
  }, [flyTo]);
}
