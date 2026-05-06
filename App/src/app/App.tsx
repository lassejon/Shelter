import { Toaster } from 'sonner';
import { ErrorBoundary } from './ErrorBoundary';
import { QueryProvider } from './providers/QueryProvider';
import { Router } from './router';
import { SessionRefresh } from './SessionRefresh';

export default function App() {
  return (
    <ErrorBoundary>
      <QueryProvider>
        <SessionRefresh />
        <Router />
        <Toaster richColors position="top-right" closeButton />
      </QueryProvider>
    </ErrorBoundary>
  );
}
