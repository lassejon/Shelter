import { format } from 'date-fns';

/** "Dec 20 - Dec 25, 2026" — same calendar year drops the second year, mixed years keeps both. */
export function formatDateRange(startUtc: string, endUtc: string): string {
  const start = new Date(startUtc);
  const end = new Date(endUtc);
  const sameYear = start.getFullYear() === end.getFullYear();
  if (sameYear) {
    return `${format(start, 'MMM d')} – ${format(end, 'MMM d, yyyy')}`;
  }
  return `${format(start, 'MMM d, yyyy')} – ${format(end, 'MMM d, yyyy')}`;
}

/** "December 20, 2026" */
export function formatDate(dateUtc: string): string {
  return format(new Date(dateUtc), 'MMMM d, yyyy');
}

/** "Dec 20, 2026" */
export function formatDateShort(dateUtc: string): string {
  return format(new Date(dateUtc), 'MMM d, yyyy');
}
