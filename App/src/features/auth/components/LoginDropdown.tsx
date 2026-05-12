import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router';
import { Calendar, House, Menu, User } from 'lucide-react';
import { AxiosError } from 'axios';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import { useLogin } from '@/features/auth/hooks/useLogin';
import { useLogout } from '@/features/auth/hooks/useLogout';
import { useResendConfirmation } from '@/features/auth/hooks/useResendConfirmation';
import { loginSchema, type LoginInput } from '@/features/auth/models/login.schema';
import type { LoginEmailNotConfirmedBody } from '@/features/auth/models/dto';
import { RegisterForm } from './RegisterForm';

type Mode = 'login' | 'register';

export function LoginDropdown() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const firstName = useAuthStore((state) => state.firstName);
  const email = useAuthStore((state) => state.email);

  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<Mode>('login');
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    function onClick(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, [open]);

  const initials = (firstName ?? email ?? '').slice(0, 1).toUpperCase();
  const greeting = firstName ?? email;

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 rounded-full px-3 py-2 transition-colors hover:bg-slate-100"
        aria-haspopup="menu"
        aria-expanded={open}
      >
        {isAuthenticated && initials ? (
          <span className="flex h-8 w-8 items-center justify-center rounded-full bg-primary-600 text-sm font-semibold text-white">
            {initials}
          </span>
        ) : (
          <User size={20} className="text-slate-600" />
        )}
        <Menu size={20} className="text-slate-600" />
      </button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 z-50 mt-2 min-w-[280px] rounded-lg border border-slate-200 bg-white p-4 shadow-xl"
        >
          {isAuthenticated ? (
            <AuthenticatedMenu greeting={greeting} onClose={() => setOpen(false)} />
          ) : (
            <UnauthenticatedMenu
              mode={mode}
              setMode={setMode}
              onSuccess={() => setOpen(false)}
            />
          )}
        </div>
      )}
    </div>
  );
}

function AuthenticatedMenu({
  greeting,
  onClose,
}: {
  greeting: string | null;
  onClose: () => void;
}) {
  const logoutMutation = useLogout();
  const roles = useAuthStore((state) => state.roles);
  const isOwner = roles.includes('ShelterOwner');

  return (
    <div className="space-y-3">
      <p className="text-sm text-slate-600">Hello, {greeting}</p>
      <div className="border-t border-slate-200" />
      <Link
        to="/settings/bookings"
        onClick={onClose}
        className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-sm text-slate-700 transition-colors hover:bg-slate-100"
      >
        <Calendar size={16} />
        <span>My Bookings</span>
      </Link>
      {isOwner && (
        <Link
          to="/settings/shelters"
          onClick={onClose}
          className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-sm text-slate-700 transition-colors hover:bg-slate-100"
        >
          <House size={16} />
          <span>Manage Shelters</span>
        </Link>
      )}
      <Link
        to="/settings/account"
        onClick={onClose}
        className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-sm text-slate-700 transition-colors hover:bg-slate-100"
      >
        <User size={16} />
        <span>Account Settings</span>
      </Link>
      <div className="border-t border-slate-200" />
      <button
        type="button"
        onClick={() => {
          logoutMutation.mutate();
          onClose();
        }}
        disabled={logoutMutation.isPending}
        className="w-full rounded-md px-3 py-2 text-left text-sm text-red-600 transition-colors hover:bg-red-50 disabled:opacity-50"
      >
        {logoutMutation.isPending ? 'Logging out…' : 'Logout'}
      </button>
    </div>
  );
}

function UnauthenticatedMenu({
  mode,
  setMode,
  onSuccess,
}: {
  mode: Mode;
  setMode: (m: Mode) => void;
  onSuccess: () => void;
}) {
  return (
    <div className="space-y-3">
      <div className="flex gap-3 text-sm">
        <button
          type="button"
          className={
            mode === 'login'
              ? 'font-semibold text-slate-900 underline underline-offset-4'
              : 'text-slate-500 transition-colors hover:text-slate-700'
          }
          onClick={() => setMode('login')}
        >
          Login
        </button>
        <button
          type="button"
          className={
            mode === 'register'
              ? 'font-semibold text-slate-900 underline underline-offset-4'
              : 'text-slate-500 transition-colors hover:text-slate-700'
          }
          onClick={() => setMode('register')}
        >
          Register
        </button>
      </div>
      {mode === 'login' ? (
        <LoginForm onSuccess={onSuccess} />
      ) : (
        <RegisterForm onSuccess={onSuccess} />
      )}
    </div>
  );
}

function LoginForm({ onSuccess }: { onSuccess: () => void }) {
  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
  });

  const loginMutation = useLogin();
  const resend = useResendConfirmation();
  const [serverError, setServerError] = useState<string | null>(null);
  const [needsConfirmation, setNeedsConfirmation] = useState(false);

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null);
    setNeedsConfirmation(false);
    try {
      await loginMutation.mutateAsync(values);
      toast.success('Logged in');
      onSuccess();
    } catch (error) {
      // 403 with `code: "email_not_confirmed"` is the API's signal that credentials are valid
      // but the account is not yet activated. Surface a distinct affordance so the user can
      // resend the confirmation without re-typing their email.
      if (error instanceof AxiosError && error.response?.status === 403) {
        const body = error.response.data as LoginEmailNotConfirmedBody | undefined;
        if (body?.code === 'email_not_confirmed') {
          setNeedsConfirmation(true);
          setServerError(body.detail ?? 'Please confirm your email before logging in.');
          return;
        }
      }
      setServerError('Invalid email or password');
      setError('password', { message: ' ' });
    }
  });

  function handleResend() {
    const email = getValues('email');
    if (!email) {
      toast.error('Type your email above first');
      return;
    }
    resend.mutate(
      { email },
      {
        onSuccess: () => toast.success('Confirmation email sent'),
        onError: () => toast.error('Could not resend the email'),
      },
    );
  }

  const inputBase =
    'w-full rounded-md border border-slate-300 px-3 py-2 text-sm placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed';

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      {serverError && (
        <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
          <p>{serverError}</p>
          {needsConfirmation && (
            <button
              type="button"
              onClick={handleResend}
              disabled={resend.isPending}
              className="mt-2 font-medium text-red-700 underline underline-offset-2 hover:text-red-800 disabled:opacity-50"
            >
              {resend.isPending ? 'Sending…' : 'Resend confirmation email'}
            </button>
          )}
        </div>
      )}
      <input
        type="email"
        placeholder="Email"
        autoComplete="email"
        disabled={isSubmitting || loginMutation.isPending}
        className={inputBase}
        {...register('email')}
      />
      {errors.email?.message && (
        <p className="text-xs text-red-600">{errors.email.message}</p>
      )}
      <input
        type="password"
        placeholder="Password"
        autoComplete="current-password"
        disabled={isSubmitting || loginMutation.isPending}
        className={inputBase}
        {...register('password')}
      />
      {errors.password?.message && errors.password.message.trim() && (
        <p className="text-xs text-red-600">{errors.password.message}</p>
      )}
      <button
        type="submit"
        disabled={isSubmitting || loginMutation.isPending}
        className="w-full rounded-md bg-primary-600 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {isSubmitting || loginMutation.isPending ? 'Logging in…' : 'Login'}
      </button>
    </form>
  );
}
