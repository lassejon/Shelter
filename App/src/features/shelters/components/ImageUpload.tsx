import { useEffect, useMemo, useRef, useState } from 'react';
import { Upload, X } from 'lucide-react';

interface ImageUploadProps {
  value: File[];
  onChange: (files: File[]) => void;
  maxImages?: number;
}

const DEFAULT_MAX = 10;

export function ImageUpload({ value, onChange, maxImages = DEFAULT_MAX }: ImageUploadProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [dragActive, setDragActive] = useState(false);

  // Generate one preview URL per file. Revoke on cleanup so blob URLs don't leak across selections.
  const previews = useMemo(() => value.map((f) => URL.createObjectURL(f)), [value]);
  useEffect(() => () => previews.forEach((url) => URL.revokeObjectURL(url)), [previews]);

  function append(files: FileList | null) {
    if (!files) return;
    const images = Array.from(files).filter((f) => f.type.startsWith('image/'));
    onChange([...value, ...images].slice(0, maxImages));
  }

  function remove(index: number) {
    onChange(value.filter((_, i) => i !== index));
  }

  function handleDrag(event: React.DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    if (event.type === 'dragenter' || event.type === 'dragover') setDragActive(true);
    else if (event.type === 'dragleave') setDragActive(false);
  }

  function handleDrop(event: React.DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    setDragActive(false);
    append(event.dataTransfer.files);
  }

  return (
    <div>
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        multiple
        onChange={(e) => append(e.target.files)}
        className="hidden"
      />

      <div
        onDragEnter={handleDrag}
        onDragLeave={handleDrag}
        onDragOver={handleDrag}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className={`cursor-pointer rounded-lg border-2 border-dashed p-8 text-center transition-colors ${
          dragActive ? 'border-primary-500 bg-primary-50' : 'border-slate-300 hover:border-slate-400'
        }`}
      >
        <Upload className="mx-auto mb-3 text-slate-400" size={32} strokeWidth={1.5} />
        <p className="mb-1 text-sm font-medium text-slate-700">Click to upload or drag and drop</p>
        <p className="text-xs text-slate-500">
          PNG, JPG, GIF up to 10MB (max {maxImages} images)
        </p>
      </div>

      {value.length > 0 && (
        <>
          <div className="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
            {previews.map((preview, index) => (
              <div key={index} className="group relative">
                <div className="aspect-square overflow-hidden rounded-lg border border-slate-200 bg-slate-100">
                  <img
                    src={preview}
                    alt={`Preview ${index + 1}`}
                    className="h-full w-full object-cover"
                  />
                </div>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    remove(index);
                  }}
                  aria-label={`Remove image ${index + 1}`}
                  className="absolute right-2 top-2 rounded-full bg-red-500 p-1 text-white opacity-0 transition-opacity hover:bg-red-600 group-hover:opacity-100"
                >
                  <X size={16} />
                </button>
                <div className="absolute bottom-2 left-2 rounded bg-black/60 px-2 py-1 text-xs text-white">
                  {index + 1}
                </div>
              </div>
            ))}
          </div>
          <p className="mt-2 text-sm text-slate-500">
            {value.length} of {maxImages} images selected
          </p>
        </>
      )}
    </div>
  );
}
