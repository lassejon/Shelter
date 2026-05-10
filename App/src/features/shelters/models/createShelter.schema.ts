import { z } from 'zod';
import { BookingApprovalMode, ShelterBookingPolicy } from '@/features/shelters/models/dto';

export const shelterFormSchema = z.object({
  name: z.string().trim().min(3, 'Name must be at least 3 characters').max(120, 'Name is too long'),
  description: z.string().trim().max(2000, 'Description is too long').optional().or(z.literal('')),
  capacity: z
    .number({ message: 'Capacity is required' })
    .int('Capacity must be a whole number')
    .min(1, 'Capacity must be at least 1')
    .max(100, 'Capacity cannot exceed 100'),
  latitude: z
    .number({ message: 'Latitude is required' })
    .min(-90, 'Latitude must be ≥ -90')
    .max(90, 'Latitude must be ≤ 90'),
  longitude: z
    .number({ message: 'Longitude is required' })
    .min(-180, 'Longitude must be ≥ -180')
    .max(180, 'Longitude must be ≤ 180'),
  // Literal-union keeps both input and output types as the discrete numeric set,
  // which is what RHF's Resolver<TFieldValues> expects for `Controller`.
  bookingPolicy: z.union([
    z.literal(ShelterBookingPolicy.ExclusiveOnly),
    z.literal(ShelterBookingPolicy.InclusiveOnly),
    z.literal(ShelterBookingPolicy.Both),
  ]),
  bookingApprovalMode: z.union([
    z.literal(BookingApprovalMode.Instant),
    z.literal(BookingApprovalMode.RequiresApproval),
  ]),
});

export type ShelterFormInput = z.infer<typeof shelterFormSchema>;

// Kept for backwards compatibility with existing imports during the rename.
export const createShelterSchema = shelterFormSchema;
export type CreateShelterInput = ShelterFormInput;
