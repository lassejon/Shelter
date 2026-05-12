import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { Mail } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { useResendConfirmation } from '@/features/auth/hooks/useResendConfirmation';

/**
 * Landing page after a successful registration. The user shouldn't be able to log in yet —
 * they need to click the link in the email we just sent. The Resend button covers the case
 * where the email never arrived (spam, typo, mail server hiccup).
 */
export default function CheckEmailPage() {
  const [searchParams] = useSearchParams();
  const email = searchParams.get('email');
  const resend = useResendConfirmation();
  const [resentTo, setResentTo] = useState<string | null>(null);

  function handleResend() {
    if (!email) return;
    resend.mutate(
      { email },
      {
        onSuccess: () => {
          setResentTo(email);
          toast.success('Confirmation email sent');
        },
        onError: () => toast.error('Could not resend the email'),
      },
    );
  }

  return (
    <div className="mx-auto max-w-lg px-4 py-12">
      <Card className="text-center" padding="lg">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary-100">
          <Mail className="h-6 w-6 text-primary-700" />
        </div>
        <h1 className="text-2xl font-semibold text-slate-900">Check your email</h1>
        {email ? (
          <p className="mt-2 text-slate-600">
            We've sent a confirmation link to <span className="font-medium text-slate-900">{email}</span>.
            Click it to activate your account, then log in.
          </p>
        ) : (
          <p className="mt-2 text-slate-600">
            We've sent you a confirmation link. Click it to activate your account, then log in.
          </p>
        )}
        <p className="mt-4 text-sm text-slate-500">
          Didn't get the email? Check your spam folder, or resend the confirmation below.
        </p>

        <div className="mt-6 flex flex-col items-center gap-3">
          {email && (
            <Button
              type="button"
              variant="secondary"
              onClick={handleResend}
              disabled={resend.isPending || resentTo === email}
            >
              {resend.isPending
                ? 'Sending…'
                : resentTo === email
                  ? 'Email sent'
                  : 'Resend confirmation email'}
            </Button>
          )}
          <Link
            to="/"
            className="text-sm font-medium text-primary-600 hover:text-primary-700"
          >
            Back to home
          </Link>
        </div>
      </Card>
    </div>
  );
}
