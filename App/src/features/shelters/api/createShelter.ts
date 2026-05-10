import { apiClient } from '@/shared/api/client';
import type { ShelterDetailResponse } from '@/features/shelters/models/dto';
import type { CreateShelterInput } from '@/features/shelters/models/createShelter.schema';

export async function createShelter(
  input: CreateShelterInput,
  pictures: File[],
): Promise<ShelterDetailResponse> {
  const form = new FormData();
  form.append('name', input.name);
  if (input.description) form.append('description', input.description);
  form.append('capacity', String(input.capacity));
  form.append('latitude', String(input.latitude));
  form.append('longitude', String(input.longitude));
  form.append('bookingPolicy', String(input.bookingPolicy));
  form.append('bookingApprovalMode', String(input.bookingApprovalMode));
  pictures.forEach((file) => form.append('pictures', file));

  const { data } = await apiClient.post<ShelterDetailResponse>('/api/shelters', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}
