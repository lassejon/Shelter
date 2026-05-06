import type { components } from '@/shared/api/types/paths';

export type ShelterDetailResponse = components['schemas']['ShelterDetailResponse'];
export type PictureResponse = components['schemas']['PictureResponse'];
export type ReviewSummary = components['schemas']['ReviewSummary'];

export const ShelterBookingPolicy = {
  ExclusiveOnly: 0,
  InclusiveOnly: 1,
  Both: 2,
} as const;

export type ShelterBookingPolicyValue =
  (typeof ShelterBookingPolicy)[keyof typeof ShelterBookingPolicy];

const policyLabels: Record<ShelterBookingPolicyValue, string> = {
  [ShelterBookingPolicy.ExclusiveOnly]: 'Exclusive only',
  [ShelterBookingPolicy.InclusiveOnly]: 'Inclusive only',
  [ShelterBookingPolicy.Both]: 'Exclusive or inclusive',
};

export function bookingPolicyLabel(policy: number): string {
  return policyLabels[policy as ShelterBookingPolicyValue] ?? 'Unknown';
}
