import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createShelter } from '@/features/shelters/api/createShelter';
import type { ShelterDetailResponse } from '@/features/shelters/models/dto';
import type { CreateShelterInput } from '@/features/shelters/models/createShelter.schema';

interface CreateShelterArgs {
  input: CreateShelterInput;
  pictures: File[];
}

export function useCreateShelter() {
  const queryClient = useQueryClient();

  return useMutation<ShelterDetailResponse, Error, CreateShelterArgs>({
    mutationFn: ({ input, pictures }) => createShelter(input, pictures),
    onSuccess: () => {
      // The new shelter affects bbox queries (it should appear on the map). Invalidating broadly is fine
      // here — bbox results are short-lived (staleTime 2m) and infrequent on a non-owner page.
      queryClient.invalidateQueries({ queryKey: ['shelters', 'bbox'] });
      queryClient.invalidateQueries({ queryKey: ['shelters', 'search'] });
    },
  });
}
