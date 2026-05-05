import { Outlet } from 'react-router';

export function SettingsLayout() {
  return (
    <div className="p-6">
      <h1 className="text-xl font-semibold text-slate-900">Settings</h1>
      <div className="mt-4">
        <Outlet />
      </div>
    </div>
  );
}
