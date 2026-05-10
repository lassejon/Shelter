import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateShelter, type UpdateShelterArgs } from '@/features/shelters/api/updateShelter';
import type { ShelterDetailResponse } from '@/features/shelters/models/dto';

export function useUpdateShelter() {
  const queryClient = useQueryClient();

  return useMutation<ShelterDetailResponse, Error, UpdateShelterArgs>({
    mutationFn: updateShelter,
    onSuccess: (updated) => {
      // Drop every cache that could be displaying this shelter's data.
      queryClient.invalidateQueries({ queryKey: ['shelters', 'mine'] });
      queryClient.invalidateQueries({ queryKey: ['shelters', 'bbox'] });
      queryClient.invalidateQueries({ queryKey: ['shelters', 'search'] });
      queryClient.setQueryData(['shelters', updated.id], updated);
    },
  });
}
