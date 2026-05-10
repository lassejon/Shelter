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

const policyDescriptions: Record<ShelterBookingPolicyValue, string> = {
  [ShelterBookingPolicy.ExclusiveOnly]: 'Reserved for one party at a time. Other guests cannot join.',
  [ShelterBookingPolicy.InclusiveOnly]: 'Shareable. Multiple parties can stay if there is remaining capacity.',
  [ShelterBookingPolicy.Both]: 'Shareable by default; guests can also book the entire shelter exclusively.',
};

export function bookingPolicyLabel(policy: number): string {
  return policyLabels[policy as ShelterBookingPolicyValue] ?? 'Unknown';
}

export function bookingPolicyDescription(policy: number): string {
  return policyDescriptions[policy as ShelterBookingPolicyValue] ?? '';
}

export const BookingApprovalMode = {
  Instant: 0,
  RequiresApproval: 1,
} as const;

export type BookingApprovalModeValue =
  (typeof BookingApprovalMode)[keyof typeof BookingApprovalMode];

const approvalLabels: Record<BookingApprovalModeValue, string> = {
  [BookingApprovalMode.Instant]: 'Instant booking',
  [BookingApprovalMode.RequiresApproval]: 'Requires approval',
};

const approvalDescriptions: Record<BookingApprovalModeValue, string> = {
  [BookingApprovalMode.Instant]:
    'Bookings are confirmed automatically. Best for hands-off owners.',
  [BookingApprovalMode.RequiresApproval]:
    'Bookings arrive as Pending; you approve or reject each one before it is confirmed.',
};

export function bookingApprovalModeLabel(mode: number): string {
  return approvalLabels[mode as BookingApprovalModeValue] ?? 'Unknown';
}

export function bookingApprovalModeDescription(mode: number): string {
  return approvalDescriptions[mode as BookingApprovalModeValue] ?? '';
}
