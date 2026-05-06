export interface PlacePrediction {
  placeId: string;
  primaryText: string;
  secondaryText?: string;
}

export interface PlaceDetails {
  latitude: number;
  longitude: number;
  name: string;
}

let sessionToken: google.maps.places.AutocompleteSessionToken | null = null;

export function initializePlacesService(): void {
  if (typeof google === 'undefined' || !google.maps?.places) return;
  try {
    sessionToken = new google.maps.places.AutocompleteSessionToken();
  } catch {
    sessionToken = null;
  }
}

interface AutocompleteSuggestionRequest {
  input: string;
  sessionToken?: google.maps.places.AutocompleteSessionToken;
  locationBias?: { center: { lat: number; lng: number }; radius: number };
}

interface PlacePredictionRaw {
  placeId?: string;
  place_id?: string;
  text?: { text?: string };
  structuredFormat?: {
    mainText?: { text?: string };
    secondaryText?: { text?: string };
  };
}

interface AutocompleteSuggestionLike {
  placePrediction?: PlacePredictionRaw;
}

interface AutocompleteSuggestionApi {
  fetchAutocompleteSuggestions: (
    request: AutocompleteSuggestionRequest,
  ) => Promise<{ suggestions: AutocompleteSuggestionLike[] }>;
}

function getAutocompleteApi(): AutocompleteSuggestionApi | null {
  if (typeof google === 'undefined' || !google.maps?.places) return null;
  const places = google.maps.places as unknown as {
    AutocompleteSuggestion?: AutocompleteSuggestionApi;
  };
  return places.AutocompleteSuggestion ?? null;
}

export async function getPlacePredictions(
  query: string,
  locationBias?: { lat: number; lng: number },
): Promise<PlacePrediction[]> {
  const trimmed = query.trim();
  if (!trimmed) return [];

  const api = getAutocompleteApi();
  if (!api) return [];

  const request: AutocompleteSuggestionRequest = {
    input: trimmed,
    sessionToken: sessionToken ?? undefined,
  };
  if (locationBias) {
    request.locationBias = {
      center: { lat: locationBias.lat, lng: locationBias.lng },
      radius: 50_000,
    };
  }

  try {
    const { suggestions } = await api.fetchAutocompleteSuggestions(request);
    return suggestions
      .filter((s): s is AutocompleteSuggestionLike & { placePrediction: PlacePredictionRaw } =>
        Boolean(s.placePrediction),
      )
      .map((s) => ({
        placeId: s.placePrediction.placeId ?? s.placePrediction.place_id ?? '',
        primaryText:
          s.placePrediction.structuredFormat?.mainText?.text ??
          s.placePrediction.text?.text ??
          '',
        secondaryText: s.placePrediction.structuredFormat?.secondaryText?.text,
      }))
      .filter((p) => p.placeId && p.primaryText);
  } catch {
    return [];
  }
}

export async function getPlaceDetails(placeId: string): Promise<PlaceDetails | null> {
  if (typeof google === 'undefined' || !google.maps?.places?.Place) return null;

  try {
    const place = new google.maps.places.Place({ id: placeId, requestedLanguage: 'en' });
    await place.fetchFields({ fields: ['location', 'displayName'] });
    sessionToken = new google.maps.places.AutocompleteSessionToken();
    if (!place.location) return null;
    return {
      latitude: place.location.lat(),
      longitude: place.location.lng(),
      name: place.displayName ?? 'Unknown',
    };
  } catch {
    sessionToken = new google.maps.places.AutocompleteSessionToken();
    return null;
  }
}
