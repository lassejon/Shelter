import { useState } from 'react';
import { Star } from 'lucide-react';
import { RATING_VALUES, type Rating } from '@/features/reviews/models/dto';

interface StarRatingProps {
  /** Current rating, 1..5 or null for unset. */
  value: Rating | null;
  onChange: (value: Rating) => void;
  size?: number;
  disabled?: boolean;
}

/**
 * Interactive 1–5 star picker. Hover preview lights up the stars up to the hovered value;
 * click commits. Read-only display lives in `ReviewCard` (just renders Star icons).
 */
export function StarRating({ value, onChange, size = 32, disabled = false }: StarRatingProps) {
  const [hover, setHover] = useState<Rating | null>(null);
  const display = hover ?? value ?? 0;

  return (
    <div className="flex items-center gap-1" role="radiogroup" aria-label="Rating">
      {RATING_VALUES.map((rating) => {
        const filled = rating <= display;
        return (
          <button
            key={rating}
            type="button"
            role="radio"
            aria-checked={value === rating}
            aria-label={`${rating} ${rating === 1 ? 'star' : 'stars'}`}
            disabled={disabled}
            onClick={() => onChange(rating)}
            onMouseEnter={() => !disabled && setHover(rating)}
            onMouseLeave={() => setHover(null)}
            className="rounded p-1 transition-transform hover:scale-110 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Star
              size={size}
              className={
                filled ? 'fill-current text-accent-400' : 'text-slate-300'
              }
            />
          </button>
        );
      })}
    </div>
  );
}
