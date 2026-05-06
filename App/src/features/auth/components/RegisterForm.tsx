import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Field } from '@/shared/ui/Field';
import { useRegister } from '@/features/auth/hooks/useRegister';
import { registerSchema, type RegisterInput } from '@/features/auth/models/register.schema';
import type { RegisterErrorBody } from '@/features/auth/models/dto';

interface RegisterFormProps {
  onSuccess: () => void;
}

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
          const list = Array.isArray(data?.errors) && data.errors.length > 0
            ? data.errors
            : [data?.detail ?? 'Registration failed.'];
          setServerErrors(list);
          return;
        }
      }
      toast.error('Could not create account');
    }
  });

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      {serverErrors.length > 0 && (
        <div className="rounded-md border border-red-200 bg-red-50 p-3">
          <ul className="list-disc space-y-1 pl-4 text-xs text-red-700">
            {serverErrors.map((message, i) => (
              <li key={i}>{message}</li>
            ))}
          </ul>
        </div>
      )}
      <div className="grid grid-cols-2 gap-2">
        <Field label="First name" required error={errors.firstName?.message}>
          {({ id, 'aria-describedby': describedBy }) => (
            <Input
              id={id}
              autoComplete="given-name"
              invalid={Boolean(errors.firstName)}
              aria-describedby={describedBy}
              {...register('firstName')}
            />
          )}
        </Field>
        <Field label="Last name" required error={errors.lastName?.message}>
          {({ id, 'aria-describedby': describedBy }) => (
            <Input
              id={id}
              autoComplete="family-name"
              invalid={Boolean(errors.lastName)}
              aria-describedby={describedBy}
              {...register('lastName')}
            />
          )}
        </Field>
      </div>
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
            autoComplete="new-password"
            invalid={Boolean(errors.password)}
            aria-describedby={describedBy}
            {...register('password')}
          />
        )}
      </Field>
      <label className="flex items-center gap-2 text-sm text-slate-700">
        <input type="checkbox" {...register('isShelterOwner')} className="h-4 w-4" />
        Register as a shelter owner
      </label>
      <Button
        type="submit"
        variant="primary"
        fullWidth
        disabled={isSubmitting || registerMutation.isPending}
      >
        {isSubmitting || registerMutation.isPending ? 'Creating account…' : 'Create account'}
      </Button>
    </form>
  );
}
