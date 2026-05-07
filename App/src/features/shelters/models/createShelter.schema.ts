import { z } from 'zod';
import { ShelterBookingPolicy } from '@/features/shelters/models/dto';

export const createShelterSchema = z.object({
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
  // Use literal-union (instead of refine on z.number()) so the inferred input *and* output types
  // are both `0 | 1 | 2`, matching what RHF's Resolver<TFieldValues> expects.
  bookingPolicy: z.union([
    z.literal(ShelterBookingPolicy.ExclusiveOnly),
    z.literal(ShelterBookingPolicy.InclusiveOnly),
    z.literal(ShelterBookingPolicy.Both),
  ]),
});

export type CreateShelterInput = z.infer<typeof createShelterSchema>;
