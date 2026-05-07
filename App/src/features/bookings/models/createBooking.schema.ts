import { z } from 'zod';
import { BookingType } from '@/features/bookings/models/dto';

export const createBookingSchema = z
  .object({
    startUtc: z.string().min(1, 'Start date is required'),
    endUtc: z.string().min(1, 'End date is required'),
    guests: z
      .number({ message: 'Guests is required' })
      .int('Guests must be a whole number')
      .min(1, 'At least 1 guest'),
    type: z.union([z.literal(BookingType.Inclusive), z.literal(BookingType.Exclusive)]),
  })
  .refine((v) => new Date(v.endUtc) > new Date(v.startUtc), {
    message: 'Check-out must be after check-in',
    path: ['endUtc'],
  });

export type CreateBookingInput = z.infer<typeof createBookingSchema>;
