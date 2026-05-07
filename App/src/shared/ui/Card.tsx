import type { HTMLAttributes, ReactNode } from 'react';

type CardElement = 'article' | 'div' | 'section';
type CardVariant = 'default' | 'accent' | 'interactive';
type CardPadding = 'none' | 'sm' | 'md' | 'lg';

interface CardProps extends HTMLAttributes<HTMLElement> {
  as?: CardElement;
  children: ReactNode;
  padding?: CardPadding;
  variant?: CardVariant;
}

const baseStyles = 'rounded-lg border';

const variantStyles: Record<CardVariant, string> = {
  default: 'border-slate-200 bg-white',
  accent: 'border-2 border-primary-200 bg-primary-50',
  interactive: 'border-slate-200 bg-white transition-shadow hover:shadow-md',
};

const paddingStyles: Record<CardPadding, string> = {
  none: '',
  sm: 'p-4',
  md: 'p-5',
  lg: 'p-6',
};

export function Card({
  as: Component = 'div',
  children,
  className = '',
  padding = 'lg',
  variant = 'default',
  ...props
}: CardProps) {
  const combined = [baseStyles, variantStyles[variant], paddingStyles[padding], className]
    .filter(Boolean)
    .join(' ');

  return (
    <Component className={combined} {...props}>
      {children}
    </Component>
  );
}
