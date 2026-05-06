import { z } from 'zod';

export const filterSchema = z
  .object({
    minRating: z.number().int().min(1, 'At least 1').max(5, 'At most 5').nullable(),
    minCapacity: z.number().int().min(1, 'Must be positive').nullable(),
    maxCapacity: z.number().int().min(1, 'Must be positive').nullable(),
  })
  .refine(
    (v) => v.minCapacity === null || v.maxCapacity === null || v.minCapacity <= v.maxCapacity,
    { message: 'Min capacity must be ≤ max capacity', path: ['maxCapacity'] },
  );

export type FilterInput = z.infer<typeof filterSchema>;
