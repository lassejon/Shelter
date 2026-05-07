import { useState } from 'react';
import { ImageIcon, Loader2, MessageSquare, Star } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import { useShelterReviews } from '@/features/reviews/hooks/useShelterReviews';
import { useMyReview } from '@/features/reviews/hooks/useMyReview';
import { useReviewPictures } from '@/features/reviews/hooks/useReviewPictures';
import type { ReviewSummary } from '@/features/shelters/models/dto';
import { ReviewCard } from './ReviewCard';
import { LeaveReviewForm } from './LeaveReviewForm';
import { PictureLightbox } from './PictureLightbox';

interface ShelterReviewsProps {
  shelterId: string;
  /** Initial summary from the shelter detail response. The paged list refresh overrides this once it lands. */
  initialSummary: ReviewSummary;
}

const PAGE_SIZE = 10;

export function ShelterReviews({ shelterId, initialSummary }: ShelterReviewsProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState(false);
  const [lightboxIndex, setLightboxIndex] = useState<number | null>(null);

  const { data: reviewsData, isLoading: reviewsLoading } = useShelterReviews(shelterId, {
    page,
    pageSize: PAGE_SIZE,
  });
  const { data: myReview, isLoading: myReviewLoading } = useMyReview(shelterId);
  const { data: picturesData, isLoading: picturesLoading } = useReviewPictures(shelterId);

  const summary = reviewsData?.summary ?? initialSummary;
  const totalCount = Number(summary.totalCount);
  const averageRating = Number(summary.averageRating);

  const others = (reviewsData?.reviews ?? []).filter(
    (r) => !myReview || r.id !== myReview.id,
  );
  const totalPages = Number(reviewsData?.pagination.totalPages ?? 0);
  const reviewPictures = picturesData?.pictures ?? [];

  return (
    <div className="rounded-lg bg-white p-6 shadow">
      <h2 className="mb-6 text-xl font-semibold text-slate-900">Reviews</h2>

      {/* Aggregate review pictures grid */}
      {picturesLoading ? (
        <div className="mb-6 flex gap-2">
          {[1, 2, 3, 4].map((i) => (
            <div
              key={i}
              className="h-20 w-20 animate-pulse rounded bg-slate-200"
            />
          ))}
        </div>
      ) : reviewPictures.length > 0 ? (
        <div className="mb-6">
          <h3 className="mb-2 text-sm font-medium text-slate-700">Guest photos</h3>
          <div className="flex flex-wrap gap-2">
            {reviewPictures.map((picture, index) => (
              <button
                key={picture.id}
                type="button"
                onClick={() => setLightboxIndex(index)}
                className="h-20 w-20 overflow-hidden rounded-lg border border-slate-200 transition-colors hover:border-primary-400"
              >
                <img
                  src={picture.url}
                  alt="Guest photo"
                  className="h-full w-full object-cover"
                />
              </button>
            ))}
          </div>
        </div>
      ) : null}

      {/* Summary */}
      <div className="mb-6 flex items-center gap-4 border-b border-slate-200 pb-6">
        {totalCount > 0 ? (
          <>
            <div className="flex items-center gap-2">
              <Star className="h-6 w-6 fill-current text-accent-400" />
              <span className="text-2xl font-semibold text-slate-900">
                {averageRating.toFixed(1)}
              </span>
            </div>
            <span className="text-slate-600">
              ({totalCount} {totalCount === 1 ? 'review' : 'reviews'})
            </span>
          </>
        ) : (
          <div className="flex items-center gap-2 text-slate-500">
            <MessageSquare className="h-5 w-5" />
            <span>No reviews yet</span>
          </div>
        )}
      </div>

      {/* Loading */}
      {(reviewsLoading || myReviewLoading) && (
        <div className="flex items-center justify-center py-8">
          <Loader2 className="h-6 w-6 animate-spin text-primary-600" />
        </div>
      )}

      {!reviewsLoading && !myReviewLoading && (
        <div className="space-y-6">
          {/* User's own review or edit form */}
          {myReview && !editing && (
            <ReviewCard
              review={myReview}
              shelterId={shelterId}
              isOwnReview
              onEdit={() => setEditing(true)}
            />
          )}
          {myReview && editing && (
            <LeaveReviewForm
              shelterId={shelterId}
              editingReview={myReview}
              onCancel={() => setEditing(false)}
              onSuccess={() => setEditing(false)}
            />
          )}

          {/* Other reviews */}
          {others.map((review) => (
            <ReviewCard
              key={review.id}
              review={review}
              shelterId={shelterId}
              isOwnReview={false}
            />
          ))}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-4 border-t border-slate-200 pt-6">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
              >
                Previous
              </Button>
              <span className="text-sm text-slate-600">
                Page {page} of {totalPages}
              </span>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
              >
                Next
              </Button>
            </div>
          )}

          {/* Empty state */}
          {!myReview && others.length === 0 && totalCount === 0 && (
            <div className="py-8 text-center text-slate-500">
              <p>Be the first to leave a review.</p>
            </div>
          )}

          {/* Leave-review form (when authenticated, no existing review, not editing) */}
          {isAuthenticated && !myReview && !editing && (
            <LeaveReviewForm shelterId={shelterId} />
          )}
        </div>
      )}

      {lightboxIndex !== null && (
        <PictureLightbox
          pictures={reviewPictures}
          initialIndex={lightboxIndex}
          onClose={() => setLightboxIndex(null)}
        />
      )}

      {/* Decorative empty-state icon under the heading when there are no reviews and no pictures */}
      {!reviewsLoading && !myReviewLoading && totalCount === 0 && reviewPictures.length === 0 && !isAuthenticated && (
        <div className="py-4 text-center text-slate-400">
          <ImageIcon className="mx-auto h-10 w-10" />
        </div>
      )}
    </div>
  );
}
