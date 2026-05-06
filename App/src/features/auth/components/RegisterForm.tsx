import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { useRegister } from '@/features/auth/hooks/useRegister';
import { registerSchema, type RegisterInput } from '@/features/auth/models/register.schema';
import type { RegisterErrorBody } from '@/features/auth/models/dto';

interface RegisterFormProps {
  onSuccess: () => void;
}

const inputBase =
  'w-full rounded-md border border-slate-300 px-3 py-2 text-sm placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed';

export function RegisterForm({ onSuccess }: RegisterFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: { isShelterOwner: false },
  });

  const registerMutation = useRegister();
  const [serverErrors, setServerErrors] = useState<string[]>([]);

  const onSubmit = handleSubmit(async (values) => {
    setServerErrors([]);
    try {
      await registerMutation.mutateAsync(values);
      toast.success('Account created');
      onSuccess();
    } catch (error) {
      if (error instanceof AxiosError && error.response) {
        const data = error.response.data as RegisterErrorBody | undefined;
        if (error.response.status === 409) {
          setServerErrors([data?.detail ?? 'An account with that email already exists.']);
          return;
        }
        if (error.response.status === 400) {
          const list =
            Array.isArray(data?.errors) && data.errors.length > 0
              ? data.errors
              : [data?.detail ?? 'Registration failed.'];
          setServerErrors(list);
          return;
        }
      }
      toast.error('Could not create account');
    }
  });

  const isPending = isSubmitting || registerMutation.isPending;

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      {serverErrors.length > 0 && (
        <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2">
          <ul className="list-disc space-y-1 pl-4 text-xs text-red-700">
            {serverErrors.map((message, i) => (
              <li key={i}>{message}</li>
            ))}
          </ul>
        </div>
      )}
      <div className="grid grid-cols-2 gap-2">
        <div>
          <input
            type="text"
            placeholder="First name"
            autoComplete="given-name"
            disabled={isPending}
            className={inputBase}
            {...register('firstName')}
          />
          {errors.firstName?.message && (
            <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p>
          )}
        </div>
        <div>
          <input
            type="text"
            placeholder="Last name"
            autoComplete="family-name"
            disabled={isPending}
            className={inputBase}
            {...register('lastName')}
          />
          {errors.lastName?.message && (
            <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p>
          )}
        </div>
      </div>
      <div>
        <input
          type="email"
          placeholder="Email"
          autoComplete="email"
          disabled={isPending}
          className={inputBase}
          {...register('email')}
        />
        {errors.email?.message && (
          <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>
        )}
      </div>
      <div>
        <input
          type="password"
          placeholder="Password"
          autoComplete="new-password"
          disabled={isPending}
          className={inputBase}
          {...register('password')}
        />
        {errors.password?.message && (
          <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>
        )}
      </div>
      <label className="flex cursor-pointer items-center gap-2 text-sm text-slate-700">
        <input
          type="checkbox"
          disabled={isPending}
          className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500 disabled:cursor-not-allowed disabled:opacity-50"
          {...register('isShelterOwner')}
        />
        Register as a shelter owner
      </label>
      <button
        type="submit"
        disabled={isPending}
        className="w-full rounded-md bg-primary-600 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {isPending ? 'Creating account…' : 'Create account'}
      </button>
    </form>
  );
}
