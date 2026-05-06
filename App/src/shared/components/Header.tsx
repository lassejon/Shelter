import { Link } from 'react-router';
import { LoginDropdown } from '@/features/auth/components/LoginDropdown';

export function Header() {
  return (
    <header className="sticky top-0 z-40 flex h-16 items-center justify-between border-b border-slate-200 bg-white px-6">
      <Link
        to="/"
        className="text-2xl font-bold text-primary-600 transition-colors hover:text-primary-700"
      >
        Shelter
      </Link>
      <LoginDropdown />
    </header>
  );
}
