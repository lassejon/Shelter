import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthResponse } from '@/features/auth/models/dto';

type AuthState = {
  userId: string | null;
  token: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  roles: string[];
  expiresAtUtc: string | null;
  isAuthenticated: boolean;
}

type AuthActions = {
  setAuth: (response: AuthResponse) => void;
  logout: () => void;
}

type AuthStore = AuthState & AuthActions;

const defaultState: AuthState = {
  userId: null,
  token: null,
  email: null,
  firstName: null,
  lastName: null,
  roles: [],
  expiresAtUtc: null,
  isAuthenticated: false,
};

export const useAuthStore = create<AuthStore>()(
  persist(
    (set) => ({
      ...defaultState,

      setAuth: (response) =>
        set({
          userId: response.userId,
          token: response.accessToken,
          email: response.email,
          firstName: response.firstName,
          lastName: response.lastName,
          roles: response.roles,
          expiresAtUtc: response.expiresAtUtc,
          isAuthenticated: true,
        }),

      logout: () => set({ ...defaultState }),
    }),
    {
      name: 'auth-storage',
    },
  ),
);
