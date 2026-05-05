import { Link } from 'react-router';

export default function NotFoundPage() {
  return (
    <div className="flex h-full items-center justify-center p-6">
      <div className="text-center">
        <h1 className="text-2xl font-semibold text-slate-900">404</h1>
        <p className="mt-2 text-sm text-slate-600">This page doesn't exist.</p>
        <Link
          to="/"
          className="mt-4 inline-block rounded-md bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700"
        >
          Go home
        </Link>
      </div>
    </div>
  );
}
