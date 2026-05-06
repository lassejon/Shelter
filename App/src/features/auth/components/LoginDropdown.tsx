import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Field } from '@/shared/ui/Field';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import { useLogin } from '@/features/auth/hooks/useLogin';
import { useLogout } from '@/features/auth/hooks/useLogout';
import { loginSchema, type LoginInput } from '@/features/auth/models/login.schema';
import { RegisterForm } from './RegisterForm';

type Mode = 'login' | 'register';

export function LoginDropdown() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const displayName = useAuthStore((state) =>
    [state.firstName, state.lastName].filter(Boolean).join(' ') || state.email,
  );

  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<Mode>('login');
  const containerRef = useRef<HTMLDivElement>(null);

  const logoutMutation = useLogout();

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

  if (isAuthenticated) {
    return (
      <div className="flex items-center gap-3">
        <span className="text-sm text-slate-700">{displayName}</span>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => logoutMutation.mutate()}
          disabled={logoutMutation.isPending}
        >
          {logoutMutation.isPending ? 'Logging out…' : 'Log out'}
        </Button>
      </div>
    );
  }

  return (
    <div ref={containerRef} className="relative">
      <Button variant="primary" size="sm" onClick={() => setOpen((v) => !v)}>
        Sign in
      </Button>
      {open && (
        <div className="absolute right-0 mt-2 w-80 rounded-lg border border-slate-200 bg-white p-4 shadow-lg z-50">
          <div className="mb-3 flex gap-2 text-sm">
            <button
              type="button"
              className={
                mode === 'login'
                  ? 'font-semibold text-slate-900 underline underline-offset-4'
                  : 'text-slate-500 hover:text-slate-700'
              }
              onClick={() => setMode('login')}
            >
              Log in
            </button>
            <button
              type="button"
              className={
                mode === 'register'
                  ? 'font-semibold text-slate-900 underline underline-offset-4'
                  : 'text-slate-500 hover:text-slate-700'
              }
              onClick={() => setMode('register')}
            >
              Register
            </button>
          </div>
          {mode === 'login' ? (
            <LoginForm onSuccess={() => setOpen(false)} />
          ) : (
            <RegisterForm onSuccess={() => setOpen(false)} />
          )}
        </div>
      )}
    </div>
  );
}

interface LoginFormProps {
  onSuccess: () => void;
}

function LoginForm({ onSuccess }: LoginFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
  });

  const loginMutation = useLogin();

  const onSubmit = handleSubmit(async (values) => {
    try {
      await loginMutation.mutateAsync(values);
      toast.success('Logged in');
      onSuccess();
    } catch {
      toast.error('Invalid email or password');
    }
  });

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <Field label="Email" required error={errors.email?.message}>
        {({ id, 'aria-describedby': describedBy }) => (
          <Input
            id={id}
            type="email"
            autoComplete="email"
            invalid={Boolean(errors.email)}
            aria-describedby={describedBy}
            {...register('email')}
          />
        )}
      </Field>
      <Field label="Password" required error={errors.password?.message}>
        {({ id, 'aria-describedby': describedBy }) => (
          <Input
            id={id}
            type="password"
            autoComplete="current-password"
            invalid={Boolean(errors.password)}
            aria-describedby={describedBy}
            {...register('password')}
          />
        )}
      </Field>
      <Button
        type="submit"
        variant="primary"
        fullWidth
        disabled={isSubmitting || loginMutation.isPending}
      >
        {isSubmitting || loginMutation.isPending ? 'Logging in…' : 'Log in'}
      </Button>
    </form>
  );
}
