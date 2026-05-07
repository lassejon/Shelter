import { forwardRef } from 'react';
import type { ButtonHTMLAttributes } from 'react';
import { Link, type LinkProps } from 'react-router';

type Variant =
  | 'primary'
  | 'secondary'
  | 'ghost'
  | 'danger'
  | 'dangerOutline'
  | 'link'
  | 'dangerLink';
type Size = 'inline' | 'sm' | 'md' | 'lg';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  fullWidth?: boolean;
}

interface LinkButtonProps extends LinkProps {
  variant?: Variant;
  size?: Size;
  fullWidth?: boolean;
}

const baseStyles =
  'inline-flex items-center justify-center gap-2 font-medium transition-colors rounded-lg focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';

const variantStyles: Record<Variant, string> = {
  primary: 'bg-primary-600 text-white hover:bg-primary-700 focus:ring-primary-500',
  secondary:
    'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 focus:ring-slate-400',
  ghost: 'bg-transparent text-slate-700 hover:bg-slate-100 focus:ring-slate-400',
  danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500',
  dangerOutline: 'border border-red-300 bg-white text-red-700 hover:bg-red-50 focus:ring-red-400',
  link: 'bg-transparent text-primary-600 hover:text-primary-700 focus:ring-primary-500',
  dangerLink: 'bg-transparent text-red-600 hover:text-red-700 focus:ring-red-500',
};

const sizeStyles: Record<Size, string> = {
  inline: 'p-0 text-sm',
  sm: 'px-3 py-1.5 text-xs',
  md: 'px-4 py-2 text-sm',
  lg: 'px-6 py-3 text-base',
};

function getButtonClassName({
  className = '',
  fullWidth = false,
  size = 'md',
  variant = 'secondary',
}: {
  className?: string;
  fullWidth?: boolean;
  size?: Size;
  variant?: Variant;
}) {
  return [
    baseStyles,
    variantStyles[variant],
    sizeStyles[size],
    fullWidth ? 'w-full' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ');
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    { variant = 'secondary', size = 'md', fullWidth = false, className = '', children, ...props },
    ref,
  ) => {
    const combined = getButtonClassName({ className, fullWidth, size, variant });

    return (
      <button ref={ref} className={combined} {...props}>
        {children}
      </button>
    );
  },
);

Button.displayName = 'Button';

export function LinkButton({
  variant = 'secondary',
  size = 'md',
  fullWidth = false,
  className = '',
  children,
  ...props
}: LinkButtonProps) {
  const combined = getButtonClassName({ className, fullWidth, size, variant });

  return (
    <Link className={combined} {...props}>
      {children}
    </Link>
  );
}
