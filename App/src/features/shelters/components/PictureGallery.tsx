import { useState } from 'react';
import { ChevronLeft, ChevronRight, MapPin } from 'lucide-react';
import type { PictureResponse } from '@/features/shelters/models/dto';

interface PictureGalleryProps {
  pictures: PictureResponse[];
  shelterName: string;
}

export function PictureGallery({ pictures, shelterName }: PictureGalleryProps) {
  const [selectedIndex, setSelectedIndex] = useState(0);

  if (pictures.length === 0) {
    return (
      <div className="flex h-96 items-center justify-center rounded-lg bg-gradient-to-br from-primary-100 to-primary-200">
        <div className="text-center">
          <MapPin className="mx-auto mb-2 h-16 w-16 text-primary-400" />
          <p className="font-medium text-primary-600">No photos available</p>
        </div>
      </div>
    );
  }

  const sorted = [...pictures].sort((a, b) => Number(a.sortOrder) - Number(b.sortOrder));
  const current = sorted[selectedIndex];

  return (
    <div>
      <div className="group relative mb-4 h-[500px] overflow-hidden rounded-lg bg-slate-200">
        <img
          src={current.url}
          alt={`${shelterName} – Photo ${selectedIndex + 1}`}
          className="h-full w-full object-cover"
        />
        {sorted.length > 1 && (
          <>
            {selectedIndex > 0 && (
              <button
                type="button"
                onClick={() => setSelectedIndex(selectedIndex - 1)}
                aria-label="Previous photo"
                className="absolute left-4 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 opacity-0 shadow-lg transition-all hover:bg-white group-hover:opacity-100"
              >
                <ChevronLeft className="h-6 w-6 text-slate-700" />
              </button>
            )}
            {selectedIndex < sorted.length - 1 && (
              <button
                type="button"
                onClick={() => setSelectedIndex(selectedIndex + 1)}
                aria-label="Next photo"
                className="absolute right-4 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 opacity-0 shadow-lg transition-all hover:bg-white group-hover:opacity-100"
              >
                <ChevronRight className="h-6 w-6 text-slate-700" />
              </button>
            )}
          </>
        )}
        <div className="absolute right-4 top-4 rounded-full bg-slate-900/70 px-3 py-1 text-sm text-white">
          {selectedIndex + 1} / {sorted.length}
        </div>
      </div>

      {sorted.length > 1 && (
        <div className="flex gap-2 overflow-x-auto pb-2">
          {sorted.map((picture, index) => (
            <button
              key={picture.id}
              type="button"
              onClick={() => setSelectedIndex(index)}
              className={`h-24 w-24 shrink-0 overflow-hidden rounded-lg border-2 transition-all ${
                index === selectedIndex
                  ? 'border-primary-600 ring-2 ring-primary-600 ring-offset-2'
                  : 'border-slate-300 hover:border-slate-400'
              }`}
            >
              <img
                src={picture.url}
                alt={`Thumbnail ${index + 1}`}
                className="h-full w-full object-cover"
              />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
