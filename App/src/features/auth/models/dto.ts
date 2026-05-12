import type { components, paths } from '@/shared/api/types/paths';

export type AuthResponse = components['schemas']['AuthResponse'];
export type LoginRequest = components['schemas']['LoginRequest'];
export type RegisterRequest = components['schemas']['RegisterRequest'];
export type RegisterResponse = components['schemas']['RegisterResponse'];
export type ConfirmEmailRequest = components['schemas']['ConfirmEmailRequest'];
export type ResendConfirmationEmailRequest =
  components['schemas']['ResendConfirmationEmailRequest'];

export type RegisterErrorBody =
  paths['/api/auth/register']['post']['responses']['400']['content']['application/json'] & {
    errors?: string[];
  };

/**
 * Body of the 403 the API returns when login is blocked by an unconfirmed email.
 * The `code` extension is what the UI keys off to show the resend-confirmation affordance.
 */
export interface LoginEmailNotConfirmedBody {
  detail?: string;
  title?: string;
  status?: number;
  code?: string;
}
