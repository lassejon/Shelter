import { z } from 'zod';

export const registerSchema = z.object({
  email: z.email('Enter a valid email'),
  password: z.string().min(1, 'Password is required'),
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  isShelterOwner: z.boolean(),
});

export type RegisterInput = z.infer<typeof registerSchema>;
