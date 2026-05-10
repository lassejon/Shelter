import { NavLink, Outlet } from 'react-router';
import { useAuthStore } from '@/features/auth/stores/auth.store';

interface SettingsTab {
  to: string;
  label: string;
  end?: boolean;
}

export function SettingsLayout() {
  const roles = useAuthStore((state) => state.roles);
  const isOwner = roles.includes('ShelterOwner');

  const tabs: SettingsTab[] = [
    { to: '/settings/account', label: 'Account' },
    { to: '/settings/bookings', label: 'My bookings' },
    ...(isOwner ? [{ to: '/settings/shelters', label: 'Manage shelters' }] : []),
  ];

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <h1 className="mb-4 text-2xl font-semibold text-slate-900">Settings</h1>
      <nav className="mb-6 border-b border-slate-200">
        <ul className="-mb-px flex flex-wrap gap-2">
          {tabs.map((tab) => (
            <li key={tab.to}>
              <NavLink
                to={tab.to}
                end={tab.end}
                className={({ isActive }) =>
                  `inline-flex items-center border-b-2 px-4 py-2 text-sm font-medium transition-colors ${
                    isActive
                      ? 'border-primary-600 text-primary-700'
                      : 'border-transparent text-slate-600 hover:border-slate-300 hover:text-slate-900'
                  }`
                }
              >
                {tab.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <Outlet />
    </div>
  );
}
