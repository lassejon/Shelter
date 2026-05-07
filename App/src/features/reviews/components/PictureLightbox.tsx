import { useEffect, useRef, useState } from 'react';
import { ChevronLeft, ChevronRight, X } from 'lucide-react';

interface LightboxPicture {
  id: string;
  url: string;
}

interface PictureLightboxProps {
  pictures: LightboxPicture[];
  initialIndex: number;
  onClose: () => void;
}

const SWIPE_THRESHOLD = 50;

export function PictureLightbox({ pictures, initialIndex, onClose }: PictureLightboxProps) {
  const [index, setIndex] = useState(() =>
    Math.min(Math.max(initialIndex, 0), pictures.length - 1),
  );
  const touchStartX = useRef<number | null>(null);

  // Clamp if the picture list shrinks while the lightbox is open (e.g. someone deletes a review).
  // setTimeout(0) keeps React 19's `set-state-in-effect` rule happy by deferring the setState.
  useEffect(() => {
    if (pictures.length === 0) {
      onClose();
      return;
    }
    const t = setTimeout(() => setIndex((i) => Math.min(i, pictures.length - 1)), 0);
    return () => clearTimeout(t);
  }, [pictures.length, onClose]);

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
      else if (event.key === 'ArrowLeft') setIndex((i) => Math.max(0, i - 1));
      else if (event.key === 'ArrowRight')
        setIndex((i) => Math.min(pictures.length - 1, i + 1));
    }
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [pictures.length, onClose]);

  if (pictures.length === 0) return null;
  const current = pictures[index];
  const hasPrev = index > 0;
  const hasNext = index < pictures.length - 1;

  function onTouchStart(event: React.TouchEvent) {
    touchStartX.current = event.touches[0]?.clientX ?? null;
  }

  function onTouchEnd(event: React.TouchEvent) {
    if (touchStartX.current === null) return;
    const endX = event.changedTouches[0]?.clientX;
    if (typeof endX !== 'number') return;
    const delta = endX - touchStartX.current;
    touchStartX.current = null;
    if (delta > SWIPE_THRESHOLD && hasPrev) setIndex((i) => i - 1);
    else if (delta < -SWIPE_THRESHOLD && hasNext) setIndex((i) => i + 1);
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-4"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-label="Picture viewer"
    >
      <button
        type="button"
        aria-label="Close"
        className="absolute right-4 top-4 z-10 text-white transition-colors hover:text-slate-300"
        onClick={(e) => {
          e.stopPropagation();
          onClose();
        }}
      >
        <X className="h-8 w-8" />
      </button>

      {pictures.length > 1 && (
        <span className="absolute left-1/2 top-4 -translate-x-1/2 rounded-full bg-black/60 px-3 py-1 text-sm font-medium text-white">
          {index + 1} / {pictures.length}
        </span>
      )}

      {hasPrev && (
        <button
          type="button"
          aria-label="Previous photo"
          className="absolute left-4 top-1/2 z-10 flex h-12 w-12 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 text-slate-800 shadow-lg transition-colors hover:bg-white"
          onClick={(e) => {
            e.stopPropagation();
            setIndex((i) => Math.max(0, i - 1));
          }}
        >
          <ChevronLeft className="h-6 w-6" />
        </button>
      )}

      {hasNext && (
        <button
          type="button"
          aria-label="Next photo"
          className="absolute right-4 top-1/2 z-10 flex h-12 w-12 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 text-slate-800 shadow-lg transition-colors hover:bg-white"
          onClick={(e) => {
            e.stopPropagation();
            setIndex((i) => Math.min(pictures.length - 1, i + 1));
          }}
        >
          <ChevronRight className="h-6 w-6" />
        </button>
      )}

      <img
        key={current.id}
        src={current.url}
        alt=""
        className="max-h-[90vh] max-w-full select-none rounded-lg object-contain"
        onClick={(e) => e.stopPropagation()}
        onTouchStart={onTouchStart}
        onTouchEnd={onTouchEnd}
        draggable={false}
      />
    </div>
  );
}
