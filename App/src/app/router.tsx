import { createBrowserRouter, Navigate, RouterProvider } from 'react-router';
import HomePage from '@/pages/HomePage';
import ShelterDetailsPage from '@/pages/ShelterDetailsPage';
import CreateShelterPage from '@/pages/CreateShelterPage';
import NotFoundPage from '@/pages/NotFoundPage';
import { SettingsLayout } from '@/pages/settings/SettingsLayout';
import { AccountSettingsPage } from '@/pages/settings/AccountSettingsPage';
import { BookingsSettingsPage } from '@/pages/settings/BookingsSettingsPage';
import { ProtectedRoute } from '@/shared/components/ProtectedRoute';
import { RoleProtectedRoute } from '@/shared/components/RoleProtectedRoute';
import { RootLayout } from './RootLayout';

const router = createBrowserRouter([
  // Map page owns its own layout (full-screen, floating MapHeader).
  { path: '/', element: <HomePage /> },
  // Other routes share the standard chrome (sticky Header + main).
  {
    element: <RootLayout />,
    children: [
      { path: '/shelters/:id', element: <ShelterDetailsPage /> },
      {
        path: '/shelters/create',
        element: (
          <RoleProtectedRoute role="ShelterOwner">
            <CreateShelterPage />
          </RoleProtectedRoute>
        ),
      },
      {
        path: '/settings',
        element: (
          <ProtectedRoute>
            <SettingsLayout />
          </ProtectedRoute>
        ),
        children: [
          { index: true, element: <Navigate to="/settings/account" replace /> },
          { path: 'account', element: <AccountSettingsPage /> },
          { path: 'bookings', element: <BookingsSettingsPage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);

export function Router() {
  return <RouterProvider router={router} />;
}
