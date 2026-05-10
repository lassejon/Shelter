import { apiClient } from '@/shared/api/client';
import type { ShelterDetailResponse } from '@/features/shelters/models/dto';
import type { ShelterFormInput } from '@/features/shelters/models/createShelter.schema';

export interface UpdateShelterArgs {
  id: string;
  /**
   * All scalar fields are sent. The backend's UpdateShelter accepts nullable values and
   * treats null as "no change", so sending current values for unchanged fields is a no-op.
   */
  input: Pick<
    ShelterFormInput,
    'name' | 'description' | 'capacity' | 'bookingPolicy' | 'bookingApprovalMode'
  > & {
    isActive?: boolean;
  };
  newPictures: File[];
  pictureIdsToDelete: string[];
}

export async function updateShelter({
  id,
  input,
  newPictures,
  pictureIdsToDelete,
}: UpdateShelterArgs): Promise<ShelterDetailResponse> {
  const form = new FormData();
  form.append('name', input.name);
  if (input.description) form.append('description', input.description);
  form.append('capacity', String(input.capacity));
  form.append('bookingPolicy', String(input.bookingPolicy));
  form.append('bookingApprovalMode', String(input.bookingApprovalMode));
  if (typeof input.isActive === 'boolean') form.append('isActive', String(input.isActive));
  pictureIdsToDelete.forEach((pid) => form.append('pictureIdsToDelete', pid));
  newPictures.forEach((file) => form.append('newPictures', file));

  const { data } = await apiClient.put<ShelterDetailResponse>(`/api/shelters/${id}`, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}
