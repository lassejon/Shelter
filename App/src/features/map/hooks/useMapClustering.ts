import { useMemo } from 'react';
import Supercluster from 'supercluster';
import type { SearchShelterResponse } from '@/features/map/models/dto';

export interface ShelterPointProperties {
  cluster: false;
  shelterId: string;
  shelter: SearchShelterResponse;
}

export interface ClusterProperties {
  cluster: true;
  cluster_id: number;
  point_count: number;
  point_count_abbreviated: string;
}

export type ClusterFeature = GeoJSON.Feature<
  GeoJSON.Point,
  ClusterProperties | ShelterPointProperties
>;

interface UseMapClusteringOptions {
  radius?: number;
  maxZoom?: number;
  minPoints?: number;
}

export function useMapClustering(
  shelters: SearchShelterResponse[],
  options: UseMapClusteringOptions = {},
) {
  const { radius = 50, maxZoom = 16, minPoints = 2 } = options;

  return useMemo(() => {
    if (!shelters || shelters.length === 0) return null;

    const index = new Supercluster<ShelterPointProperties, ClusterProperties>({
      radius,
      maxZoom,
      minPoints,
    });

    const points: GeoJSON.Feature<GeoJSON.Point, ShelterPointProperties>[] = shelters.map(
      (shelter) => ({
        type: 'Feature',
        geometry: {
          type: 'Point',
          coordinates: [Number(shelter.longitude), Number(shelter.latitude)],
        },
        properties: {
          cluster: false,
          shelterId: shelter.id,
          shelter,
        },
      }),
    );

    index.load(points);
    return index;
  }, [shelters, radius, maxZoom, minPoints]);
}

export function getClustersForViewport(
  supercluster: Supercluster<ShelterPointProperties, ClusterProperties> | null,
  bbox: [number, number, number, number],
  zoom: number,
): ClusterFeature[] {
  if (!supercluster) return [];
  return supercluster.getClusters(bbox, Math.floor(zoom)) as ClusterFeature[];
}

export function getClusterExpansionZoom(
  supercluster: Supercluster<ShelterPointProperties, ClusterProperties> | null,
  clusterId: number,
): number {
  if (!supercluster) return 20;
  return supercluster.getClusterExpansionZoom(clusterId);
}
