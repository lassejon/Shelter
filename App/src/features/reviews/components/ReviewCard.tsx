import { useState } from 'react';
import { Pencil, Star, Trash2, User } from 'lucide-react';
import { toast } from 'sonner';
import { useDeleteReview } from '@/features/reviews/hooks/useDeleteReview';
import { formatDate } from '@/shared/utils/date';
import type { ReviewDetailResponse } from '@/features/reviews/models/dto';
import { PictureLightbox } from './PictureLightbox';

interface ReviewCardProps {
  review: ReviewDetailResponse;
  shelterId: string;
  isOwnReview?: boolean;
  onEdit?: () => void;
}

export function ReviewCard({
  review,
  shelterId,
  isOwnReview = false,
  onEdit,
}: ReviewCardProps) {
  const [lightboxIndex, setLightboxIndex] = useState<number | null>(null);
  const [confirming, setConfirming] = useState(false);
  const deleteMutation = useDeleteReview();

  const rating = Number(review.rating);

  function handleDelete() {
    deleteMutation.mutate(
      { reviewId: review.id, shelterId },
      {
        onSuccess: () => {
          setConfirming(false);
          toast.success('Review deleted');
        },
        onError: () => toast.error('Could not delete review'),
      },
    );
  }

  const wrapperClass = isOwnReview
    ? '-mx-4 rounded-lg border-2 border-primary-200 bg-primary-50 px-4 py-4'
    : 'border-b border-slate-200 pb-6 last:border-0 last:pb-0';

  return (
    <div className={wrapperClass}>
      {isOwnReview && (
        <div className="mb-3 flex items-center justify-between">
          <span className="inline-flex items-center gap-1 rounded-full bg-primary-100 px-2 py-1 text-xs font-medium text-primary-800">
            <User className="h-3 w-3" />
            Your review
          </span>
          <div className="flex items-center gap-2">
            {onEdit && (
              <button
                type="button"
                onClick={onEdit}
                title="Edit review"
                className="rounded-lg p-2 text-primary-600 transition-colors hover:bg-primary-100"
              >
                <Pencil className="h-4 w-4" />
              </button>
            )}
            <button
              type="button"
              onClick={() => setConfirming(true)}
              title="Delete review"
              className="rounded-lg p-2 text-red-600 transition-colors hover:bg-red-100"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}

      {confirming && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-4">
          <p className="mb-3 text-sm font-medium text-red-800">
            Delete your review? This can't be undone.
          </p>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              className="rounded bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
            >
              {deleteMutation.isPending ? 'Deleting…' : 'Yes, delete'}
            </button>
            <button
              type="button"
              onClick={() => setConfirming(false)}
              className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      <div className="mb-3 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div
            className={`flex h-10 w-10 items-center justify-center rounded-full ${
              isOwnReview ? 'bg-primary-200' : 'bg-primary-100'
            }`}
          >
            <User className={`h-5 w-5 ${isOwnReview ? 'text-primary-700' : 'text-primary-600'}`} />
          </div>
          <div>
            <div className="font-medium text-slate-900">
              {review.reviewerName ?? 'Anonymous'}
            </div>
            <div className="text-xs text-slate-500">{formatDate(review.createdAt)}</div>
          </div>
        </div>
        <div className="flex items-center gap-1">
          {[1, 2, 3, 4, 5].map((star) => (
            <Star
              key={star}
              className={`h-4 w-4 ${
                star <= rating ? 'fill-current text-accent-400' : 'text-slate-300'
              }`}
            />
          ))}
        </div>
      </div>

      {review.comment && (
        <p className="mb-3 whitespace-pre-wrap text-slate-700">{review.comment}</p>
      )}

      {review.pictures.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {review.pictures.map((picture, index) => (
            <button
              key={picture.id}
              type="button"
              onClick={() => setLightboxIndex(index)}
              className="h-24 w-24 overflow-hidden rounded-lg border border-slate-200 transition-colors hover:border-primary-400"
            >
              <img
                src={picture.url}
                alt="Review photo"
                className="h-full w-full object-cover"
              />
            </button>
          ))}
        </div>
      )}

      {lightboxIndex !== null && (
        <PictureLightbox
          pictures={review.pictures}
          initialIndex={lightboxIndex}
          onClose={() => setLightboxIndex(null)}
        />
      )}
    </div>
  );
}
