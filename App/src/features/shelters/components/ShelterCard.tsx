import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router';
import { Calendar, ChevronLeft, ChevronRight, MapPin, Star, Users, X } from 'lucide-react';
import type { SearchShelterResponse } from '@/features/map/models/dto';
import { bookingPolicyLabel } from '@/features/shelters/models/dto';

interface ShelterCardProps {
  shelter: SearchShelterResponse;
  position: { x: number; y: number };
  onClose: () => void;
}

const CARD_WIDTH = 320;
const CARD_HEIGHT = 400;

function calculatePosition(markerX: number, markerY: number) {
  const offset = 20;
  let left = markerX + offset;
  let top = markerY + offset;

  if (left + CARD_WIDTH > window.innerWidth - 20) {
    left = markerX - CARD_WIDTH - offset;
  }
  if (top + CARD_HEIGHT > window.innerHeight - 20) {
    top = markerY - CARD_HEIGHT - offset;
  }
  // Keep clear of the floating MapHeader (h-20 + 20px margin = 100, leave 84 to match prior).
  left = Math.max(20, Math.min(left, window.innerWidth - CARD_WIDTH - 20));
  top = Math.max(84, Math.min(top, window.innerHeight - CARD_HEIGHT - 20));
  return { top, left };
}

export function ShelterCard({ shelter, position, onClose }: ShelterCardProps) {
  const [cardPosition, setCardPosition] = useState(() =>
    calculatePosition(position.x, position.y),
  );

  useEffect(() => {
    const onResize = () => setCardPosition(calculatePosition(position.x, position.y));
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, [position.x, position.y]);

  const reviewCount = Number(shelter.reviewSummary.totalCount);
  const reviewAvg = Number(shelter.reviewSummary.averageRating);
  const description = shelter.description ?? null;

  return (
    <>
      {/* Transparent backdrop — click anywhere outside the card to close. */}
      <div className="fixed inset-0 z-[90]" onClick={onClose} />

      <div
        className="fixed z-[100] w-80 overflow-hidden rounded-lg bg-white shadow-2xl"
        style={{ top: `${cardPosition.top}px`, left: `${cardPosition.left}px` }}
      >
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onClose();
          }}
          aria-label="Close"
          className="absolute right-2 top-2 z-10 flex h-8 w-8 items-center justify-center rounded-full bg-white/90 shadow-md transition-colors hover:bg-white"
        >
          <X className="h-5 w-5 text-slate-700" />
        </button>

        <Link
          to={`/shelters/${shelter.id}`}
          className="block transition-colors hover:bg-slate-50"
          onClick={(e) => e.stopPropagation()}
        >
          <PhotoSwiper pictures={shelter.pictures} />

          <div className="p-4">
            <h3 className="mb-2 text-lg font-semibold text-slate-900">{shelter.name}</h3>

            <div className="mb-3 flex items-center gap-2">
              {reviewCount > 0 ? (
                <>
                  <div className="flex items-center gap-1 text-accent-500">
                    <Star className="h-4 w-4 fill-current" />
                    <span className="text-sm font-semibold">{reviewAvg.toFixed(1)}</span>
                  </div>
                  <span className="text-sm text-slate-600">
                    ({reviewCount} {reviewCount === 1 ? 'review' : 'reviews'})
                  </span>
                </>
              ) : (
                <span className="text-sm text-slate-500">No reviews yet</span>
              )}
            </div>

            {description && (
              <p className="mb-4 text-sm text-slate-700">{truncate(description, 150)}</p>
            )}

            <div className="space-y-2 text-sm text-slate-700">
              <div className="flex items-center gap-2">
                <Users className="h-4 w-4 text-primary-600" />
                <span>Capacity: {Number(shelter.capacity)} guests</span>
              </div>
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4 text-primary-600" />
                <span>{bookingPolicyLabel(Number(shelter.bookingPolicy))}</span>
              </div>
            </div>
          </div>
        </Link>
      </div>
    </>
  );
}

function truncate(text: string, max: number): string {
  if (text.length <= max) return text;
  return text.slice(0, max) + '…';
}

function PhotoSwiper({
  pictures,
}: {
  pictures: SearchShelterResponse['pictures'];
}) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [currentIndex, setCurrentIndex] = useState(0);

  if (!pictures || pictures.length === 0) {
    return (
      <div className="flex h-60 items-center justify-center bg-gradient-to-br from-primary-100 to-primary-200">
        <div className="text-center">
          <MapPin className="mx-auto mb-2 h-12 w-12 text-primary-400" />
          <p className="text-sm font-medium text-primary-600">No photos available</p>
        </div>
      </div>
    );
  }

  function navigate(index: number) {
    if (!scrollRef.current) return;
    scrollRef.current.scrollTo({
      left: scrollRef.current.offsetWidth * index,
      behavior: 'smooth',
    });
  }

  function onScroll() {
    if (!scrollRef.current) return;
    const index = Math.round(scrollRef.current.scrollLeft / scrollRef.current.offsetWidth);
    setCurrentIndex(index);
  }

  return (
    <div className="group relative">
      <div
        ref={scrollRef}
        onScroll={onScroll}
        className="flex h-60 snap-x snap-mandatory overflow-x-auto"
        style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' as const }}
      >
        {pictures.map((pic, i) => (
          <img
            key={pic.id}
            src={pic.url}
            alt={`Photo ${i + 1}`}
            className="h-60 w-full shrink-0 snap-center object-cover"
          />
        ))}
      </div>

      {pictures.length > 1 && currentIndex > 0 && (
        <button
          type="button"
          onClick={(e) => {
            e.preventDefault();
            e.stopPropagation();
            navigate(currentIndex - 1);
          }}
          aria-label="Previous photo"
          className="absolute left-2 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 opacity-0 shadow-md transition-all hover:bg-white group-hover:opacity-100"
        >
          <ChevronLeft className="h-5 w-5 text-slate-700" />
        </button>
      )}
      {pictures.length > 1 && currentIndex < pictures.length - 1 && (
        <button
          type="button"
          onClick={(e) => {
            e.preventDefault();
            e.stopPropagation();
            navigate(currentIndex + 1);
          }}
          aria-label="Next photo"
          className="absolute right-2 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 opacity-0 shadow-md transition-all hover:bg-white group-hover:opacity-100"
        >
          <ChevronRight className="h-5 w-5 text-slate-700" />
        </button>
      )}

      {pictures.length > 1 && (
        <div className="absolute bottom-2 left-1/2 flex -translate-x-1/2 gap-2">
          {pictures.map((_, i) => (
            <span
              key={i}
              className={`h-2 w-2 rounded-full transition-colors ${
                i === currentIndex ? 'bg-white' : 'bg-white/50'
              }`}
            />
          ))}
        </div>
      )}
    </div>
  );
}
