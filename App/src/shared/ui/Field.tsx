import { useId } from 'react';
import type { ReactElement } from 'react';

interface FieldProps {
  label: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: (props: { id: string; 'aria-describedby': string | undefined }) => ReactElement;
}

export function Field({ label, error, hint, required, children }: FieldProps) {
  const id = useId();
  const messageId = error || hint ? `${id}-msg` : undefined;

  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {label}
        {required && <span className="ml-0.5 text-red-500">*</span>}
      </label>
      {children({ id, 'aria-describedby': messageId })}
      {(error || hint) && (
        <p id={messageId} className={error ? 'text-xs text-red-600' : 'text-xs text-slate-500'}>
          {error || hint}
        </p>
      )}
    </div>
  );
}
