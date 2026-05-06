import type { components, paths } from '@/shared/api/types/paths';

export type AuthResponse = components['schemas']['AuthResponse'];
export type LoginRequest = components['schemas']['LoginRequest'];
export type RegisterRequest = components['schemas']['RegisterRequest'];

export type RegisterErrorBody =
  paths['/api/auth/register']['post']['responses']['400']['content']['application/json'] & {
    errors?: string[];
  };
