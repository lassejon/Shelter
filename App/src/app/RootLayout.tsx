import { Outlet } from 'react-router';
import { Header } from '@/shared/components/Header';

export function RootLayout() {
  return (
    <div className="flex h-full flex-col">
      <Header />
      <main className="flex-1 overflow-auto">
        <Outlet />
      </main>
    </div>
  );
}
