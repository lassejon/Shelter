import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  userId: string | null;
  token: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  roles: string[];
  expiresAtUtc: string | null;
  isAuthenticated: boolean;
}

interface AuthActions {
  setAuth: (data: {
    userId: string;
    token: string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    roles: string[];
    expiresAtUtc: string;
  }) => void;
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

      setAuth: (data) =>
        set({
          userId: data.userId,
          token: data.token,
          email: data.email,
          firstName: data.firstName,
          lastName: data.lastName,
          roles: data.roles,
          expiresAtUtc: data.expiresAtUtc,
          isAuthenticated: true,
        }),

      logout: () => set({ ...defaultState }),
    }),
    {
      name: 'auth-storage',
    },
  ),
);
