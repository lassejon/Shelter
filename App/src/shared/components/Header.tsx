import { Link } from 'react-router';
import { LoginDropdown } from '@/features/auth/components/LoginDropdown';
import { useAuthStore } from '@/features/auth/stores/auth.store';

export function Header() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (
    <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
      <Link to="/" className="text-base font-semibold text-slate-900">
        Shelter
      </Link>
      <nav className="flex items-center gap-4">
        {isAuthenticated && (
          <Link to="/settings" className="text-sm text-slate-700 hover:text-slate-900">
            Settings
          </Link>
        )}
        <LoginDropdown />
      </nav>
    </header>
  );
}
