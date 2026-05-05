import { forwardRef } from 'react';
import type { InputHTMLAttributes } from 'react';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean;
}

const baseStyles =
  'w-full rounded-md border bg-white px-3 py-2 text-sm placeholder-slate-400 focus:outline-none focus:ring-2 disabled:cursor-not-allowed disabled:opacity-50';

const validStyles = 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-500';
const invalidStyles = 'border-red-500 focus:border-red-500 focus:ring-red-500';

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ invalid = false, className = '', ...props }, ref) => {
    const combined = [baseStyles, invalid ? invalidStyles : validStyles, className]
      .filter(Boolean)
      .join(' ');

    return <input ref={ref} className={combined} aria-invalid={invalid || undefined} {...props} />;
  },
);

Input.displayName = 'Input';
