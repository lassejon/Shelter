import { Star } from 'lucide-react';
import type { ReviewSummary } from '@/features/shelters/models/dto';

interface ReviewSummaryBadgeProps {
  summary: ReviewSummary;
}

export function ReviewSummaryBadge({ summary }: ReviewSummaryBadgeProps) {
  const count = Number(summary.totalCount);
  const average = Number(summary.averageRating);

  if (count === 0) {
    return <span className="text-slate-500">No reviews yet</span>;
  }

  return (
    <div className="flex items-center gap-2">
      <div className="flex items-center gap-1 text-accent-500">
        <Star className="h-5 w-5 fill-current" />
        <span className="font-semibold">{average.toFixed(1)}</span>
      </div>
      <span className="text-slate-600">
        ({count} {count === 1 ? 'review' : 'reviews'})
      </span>
    </div>
  );
}
