import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { CheckCircle2, Loader2, XCircle } from 'lucide-react';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { Button, LinkButton } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { useConfirmEmail } from '@/features/auth/hooks/useConfirmEmail';

type State = 'pending' | 'success' | 'invalid-link' | 'invalid-token' | 'error';

/**
 * Lands here from the confirmation email. Parses userId + token from the query string and
 * fires the confirm-email mutation once. On success the API returns a fresh AuthResponse and
 * the user is signed in; we redirect to the homepage with a success toast. On failure we show
 * an inline error and offer paths back into the auth flow.
 */
export default function ConfirmEmailPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const confirm = useConfirmEmail();
  const userId = searchParams.get('userId');
  const token = searchParams.get('token');
  // Initial state derived from the URL up front so the effect never has to call setState
  // synchronously to record "the link was malformed" — async setState in the mutation
  // callbacks below is fine because the effect has already returned by then.
  const [state, setState] = useState<State>(() =>
    !userId || !token ? 'invalid-link' : 'pending',
  );
  // Run the mutation exactly once even under StrictMode's double-mount.
  const startedRef = useRef(false);

  useEffect(() => {
    if (startedRef.current || !userId || !token) return;
    startedRef.current = true;

    // mutateAsync (Promise-based) instead of mutate(..., { onSuccess }) — the per-call
    // callbacks are bound to the mutation observer, which StrictMode tears down on the
    // dev-time double-mount cleanup, so they silently never fire. The Promise resolves
    // independent of observer lifecycle.
    confirm
      .mutateAsync({ userId, token })
      .then(() => {
        setState('success');
        toast.success('Email confirmed — welcome!');
        // Small delay so the user sees the success state before the redirect.
        setTimeout(() => navigate('/', { replace: true }), 1200);
      })
      .catch((error: unknown) => {
        if (error instanceof AxiosError && error.response?.status === 400) {
          setState('invalid-token');
        } else {
          setState('error');
        }
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="mx-auto max-w-lg px-4 py-12">
      <Card className="text-center" padding="lg">
        {state === 'pending' && (
          <>
            <Loader2 className="mx-auto mb-4 h-10 w-10 animate-spin text-primary-600" />
            <h1 className="text-2xl font-semibold text-slate-900">Confirming your email…</h1>
            <p className="mt-2 text-slate-600">Hang tight, this only takes a moment.</p>
          </>
        )}

        {state === 'success' && (
          <>
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary-100">
              <CheckCircle2 className="h-6 w-6 text-primary-700" />
            </div>
            <h1 className="text-2xl font-semibold text-slate-900">Email confirmed</h1>
            <p className="mt-2 text-slate-600">You're now signed in. Redirecting…</p>
          </>
        )}

        {(state === 'invalid-link' || state === 'invalid-token') && (
          <>
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
              <XCircle className="h-6 w-6 text-red-700" />
            </div>
            <h1 className="text-2xl font-semibold text-slate-900">
              {state === 'invalid-link'
                ? 'Confirmation link is incomplete'
                : 'Link expired or already used'}
            </h1>
            <p className="mt-2 text-slate-600">
              {state === 'invalid-link'
                ? 'The URL is missing the userId or token. Try clicking the link in your email again.'
                : 'This confirmation link is no longer valid. Request a new one and try again.'}
            </p>
            <div className="mt-6 flex flex-col items-center gap-2">
              <LinkButton to="/" variant="primary">
                Back to home
              </LinkButton>
              <Link
                to="/"
                className="text-sm font-medium text-primary-600 hover:text-primary-700"
              >
                Open the menu and pick "Resend confirmation"
              </Link>
            </div>
          </>
        )}

        {state === 'error' && (
          <>
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
              <XCircle className="h-6 w-6 text-red-700" />
            </div>
            <h1 className="text-2xl font-semibold text-slate-900">Something went wrong</h1>
            <p className="mt-2 text-slate-600">
              We couldn't confirm your email right now. Please try again in a moment.
            </p>
            <div className="mt-6">
              <Button type="button" variant="primary" onClick={() => window.location.reload()}>
                Try again
              </Button>
            </div>
          </>
        )}
      </Card>
    </div>
  );
}
