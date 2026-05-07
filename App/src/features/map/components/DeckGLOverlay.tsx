import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useMap } from '@vis.gl/react-google-maps';
import { GoogleMapsOverlay } from '@deck.gl/google-maps';
import { IconLayer, ScatterplotLayer, TextLayer } from '@deck.gl/layers';
import type { Layer } from '@deck.gl/core';
import iconMapping from '@/assets/markers/iconMapping.json';
import {
  getClusterExpansionZoom,
  getClustersForViewport,
  useMapClustering,
  type ClusterFeature,
  type ShelterPointProperties,
} from '@/features/map/hooks/useMapClustering';
import type { BoundingBox, SearchShelterResponse } from '@/features/map/models/dto';

interface DeckGLOverlayProps {
  shelters?: SearchShelterResponse[];
  /** Debounced bbox from useMapViewport. Drives clustering. Same source as the bbox query. */
  bbox: BoundingBox | null;
  /** Debounced zoom from useMapViewport. */
  zoom: number;
  onShelterClick?: (data: { shelter: SearchShelterResponse; x: number; y: number }) => void;
  onClusterClick?: (data: {
    clusterId: number;
    center: [number, number];
    expansionZoom: number;
  }) => void;
}

interface PickInfo {
  object?: ClusterFeature;
  x: number;
  y: number;
}

export function DeckGLOverlay({
  shelters = [],
  bbox,
  zoom,
  onShelterClick,
  onClusterClick,
}: DeckGLOverlayProps) {
  const map = useMap();
  const [overlay, setOverlay] = useState<GoogleMapsOverlay | null>(null);
  const [clickedShelterId, setClickedShelterId] = useState<string | null>(null);
  const [hoverInfo, setHoverInfo] = useState<{ object?: ClusterFeature } | null>(null);
  const markerClickedRef = useRef(false);

  const supercluster = useMapClustering(shelters, { radius: 50, maxZoom: 16, minPoints: 2 });

  const clusters = useMemo(() => {
    if (!bbox || !supercluster) return [];
    // supercluster expects [west, south, east, north]; useMapViewport gives the equivalent
    // padded bbox in BoundingBox shape.
    const tuple: [number, number, number, number] = [
      bbox.minLongitude,
      bbox.minLatitude,
      bbox.maxLongitude,
      bbox.maxLatitude,
    ];
    return getClustersForViewport(supercluster, tuple, zoom);
  }, [supercluster, bbox, zoom]);

  const { clusterData, shelterData } = useMemo(() => {
    const c: ClusterFeature[] = [];
    const s: ClusterFeature[] = [];
    clusters.forEach((feature) => {
      if (feature.properties.cluster) c.push(feature);
      else s.push(feature);
    });
    return { clusterData: c, shelterData: s };
  }, [clusters]);

  // Initialize overlay once when map is ready.
  useEffect(() => {
    if (!map || overlay) return;
    google.maps.event.addListenerOnce(map, 'idle', () => {
      const deckOverlay = new GoogleMapsOverlay({
        // Keep deck.gl in its own canvas so marker layer updates do not disturb
        // the Google vector map's shared WebGL context after bbox fetches.
        interleaved: false,
        layers: [],
      });
      deckOverlay.setMap(map);
      setOverlay(deckOverlay);
    });
  }, [map, overlay]);

  useEffect(() => {
    return () => {
      if (overlay) overlay.setMap(null);
    };
  }, [overlay]);

  // Click on background clears the selection (unless a marker was just clicked).
  useEffect(() => {
    if (!map) return;
    const listener = map.addListener('click', () => {
      setTimeout(() => {
        if (!markerClickedRef.current) setClickedShelterId(null);
        markerClickedRef.current = false;
      }, 0);
    });
    return () => google.maps.event.removeListener(listener);
  }, [map]);

  const handleClusterClick = useCallback(
    (info: PickInfo) => {
      if (!info.object || !info.object.properties.cluster) return;
      const clusterId = info.object.properties.cluster_id;
      const center = info.object.geometry.coordinates as [number, number];
      const expansionZoom = getClusterExpansionZoom(supercluster, clusterId);
      if (map) {
        map.panTo({ lat: center[1], lng: center[0] });
        map.setZoom(Math.min(expansionZoom, 18));
      }
      onClusterClick?.({ clusterId, center, expansionZoom });
    },
    [map, supercluster, onClusterClick],
  );

  const handleShelterClick = useCallback(
    (info: PickInfo) => {
      if (!info.object || info.object.properties.cluster) return;
      const props = info.object.properties as ShelterPointProperties;
      markerClickedRef.current = true;
      setClickedShelterId(props.shelter.id);
      onShelterClick?.({ shelter: props.shelter, x: info.x, y: info.y });
    },
    [onShelterClick],
  );

  const onClickCallback = useCallback(
    (info: PickInfo) => {
      if (!info.object) return;
      if (info.object.properties.cluster) handleClusterClick(info);
      else handleShelterClick(info);
    },
    [handleClusterClick, handleShelterClick],
  );

  const getSizeCallback = useCallback(
    (d: ClusterFeature) => {
      if (d.properties.cluster) return 0;
      const props = d.properties as ShelterPointProperties;
      const isClicked = clickedShelterId === props.shelterId;
      const hovered = hoverInfo?.object?.properties as ShelterPointProperties | undefined;
      const isHovered = hovered && !hovered.cluster && hovered.shelterId === props.shelterId;
      if (isClicked) return 56;
      if (isHovered) return 44;
      return 38;
    },
    [clickedShelterId, hoverInfo],
  );

  const getPixelOffsetCallback = useCallback(
    (d: ClusterFeature): [number, number] => {
      if (d.properties.cluster) return [0, 0];
      const props = d.properties as ShelterPointProperties;
      if (clickedShelterId === props.shelterId) return [0, -13];
      return [0, 0];
    },
    [clickedShelterId],
  );

  const onHoverCallback = useCallback((info: { object?: ClusterFeature }) => {
    setHoverInfo(info.object ? info : null);
  }, []);

  // Rebuild layers on every relevant state change. With `interleaved: false` on the overlay,
  // these reuploads are isolated to deck.gl's own canvas — they don't disturb Google Maps.
  // The viewport debounce in useMapViewport ensures this fires once per pan, not on every idle.
  useEffect(() => {
    if (!overlay) return;
    const layers: Layer[] = [];

    if (clusterData.length > 0) {
      layers.push(
        new ScatterplotLayer({
          id: 'cluster-layer',
          data: clusterData,
          getPosition: (d: ClusterFeature) => d.geometry.coordinates as [number, number],
          getRadius: (d: ClusterFeature) => {
            const count = d.properties.cluster ? d.properties.point_count : 1;
            return Math.min(50, Math.max(20, 10 + Math.sqrt(count) * 4));
          },
          getFillColor: [16, 185, 129, 230],
          getLineColor: [255, 255, 255, 255],
          lineWidthMinPixels: 2,
          pickable: true,
          onClick: onClickCallback,
          onHover: onHoverCallback,
          radiusUnits: 'pixels',
        }),
      );

      layers.push(
        new TextLayer({
          id: 'cluster-text-layer',
          data: clusterData,
          getPosition: (d: ClusterFeature) => d.geometry.coordinates as [number, number],
          getText: (d: ClusterFeature) => {
            if (!d.properties.cluster) return '';
            const count = d.properties.point_count;
            if (count >= 1000) return `${Math.round(count / 1000)}k`;
            return String(count);
          },
          getSize: 14,
          getColor: [255, 255, 255, 255],
          getTextAnchor: 'middle',
          getAlignmentBaseline: 'center',
          fontFamily: 'system-ui, -apple-system, sans-serif',
          fontWeight: 'bold',
          pickable: false,
        }),
      );
    }

    if (shelterData.length > 0) {
      layers.push(
        new IconLayer({
          id: 'shelters-layer',
          data: shelterData,
          iconAtlas: '/markers/shelter-marker.png',
          iconMapping,
          getIcon: () => 'shelter',
          getPosition: (d: ClusterFeature) => d.geometry.coordinates as [number, number],
          getSize: getSizeCallback,
          getPixelOffset: getPixelOffsetCallback,
          sizeUnits: 'pixels',
          pickable: true,
          onHover: onHoverCallback,
          onClick: onClickCallback,
          updateTriggers: {
            getSize: [clickedShelterId, hoverInfo],
            getPixelOffset: [clickedShelterId],
          },
          transitions: {
            getPixelOffset: { duration: 200, easing: (t: number) => t * (2 - t) },
          },
        }),
      );
    }

    if (clickedShelterId) {
      const clickedFeature = shelterData.find(
        (f) =>
          !f.properties.cluster &&
          (f.properties as ShelterPointProperties).shelterId === clickedShelterId,
      );
      if (clickedFeature) {
        layers.push(
          new ScatterplotLayer({
            id: 'marker-dot-layer',
            data: [clickedFeature],
            getPosition: (d: ClusterFeature) => d.geometry.coordinates as [number, number],
            radiusScale: 1,
            radiusMinPixels: 3,
            radiusMaxPixels: 3,
            getFillColor: [212, 165, 116, 255],
            getLineColor: [255, 255, 255, 255],
            lineWidthMinPixels: 1,
            pickable: false,
          }),
        );
      }
    }

    overlay.setProps({ layers });
  }, [
    overlay,
    clusterData,
    shelterData,
    clickedShelterId,
    hoverInfo,
    getSizeCallback,
    getPixelOffsetCallback,
    onHoverCallback,
    onClickCallback,
  ]);

  return null;
}
