import { z } from 'zod';

export const createReviewSchema = z.object({
  rating: z.union([
    z.literal(1),
    z.literal(2),
    z.literal(3),
    z.literal(4),
    z.literal(5),
  ]),
  comment: z
    .string()
    .trim()
    .max(2000, 'Comment is too long')
    .optional()
    .or(z.literal('')),
});

export type CreateReviewInput = z.infer<typeof createReviewSchema>;
