import { Toaster } from 'sonner';
import { ErrorBoundary } from './ErrorBoundary';
import { QueryProvider } from './providers/QueryProvider';
import { Router } from './router';

export default function App() {
  return (
    <ErrorBoundary>
      <QueryProvider>
        <Router />
        <Toaster richColors position="top-right" closeButton />
      </QueryProvider>
    </ErrorBoundary>
  );
}
