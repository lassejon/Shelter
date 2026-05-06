import { useEffect } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { Star, X } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMapFilterStore } from '@/shared/stores/map-filter.store';
import { filterSchema, type FilterInput } from '@/features/search/models/filter.schema';

interface FilterModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function FilterModal({ isOpen, onClose }: FilterModalProps) {
  const filters = useMapFilterStore((s) => s.filters);
  const setFilters = useMapFilterStore((s) => s.setFilters);
  const clearAll = useMapFilterStore((s) => s.clearAll);

  const {
    handleSubmit,
    reset,
    control,
    formState: { errors },
  } = useForm<FilterInput>({
    resolver: zodResolver(filterSchema),
    defaultValues: filters,
  });

  useEffect(() => {
    if (isOpen) reset(filters);
  }, [isOpen, filters, reset]);

  const onSubmit = handleSubmit((values) => {
    setFilters(values);
    onClose();
  });

  const onClear = () => {
    clearAll();
    reset({ minRating: null, minCapacity: null, maxCapacity: null });
  };

  return (
    <Dialog.Root open={isOpen} onOpenChange={(o) => !o && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 max-h-[90vh] w-full max-w-2xl -translate-x-1/2 -translate-y-1/2 overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
          <div className="mb-6 flex items-center justify-between">
            <Dialog.Title className="text-xl font-semibold text-slate-900">Filters</Dialog.Title>
            <Dialog.Close asChild>
              <button
                type="button"
                aria-label="Close"
                className="text-slate-400 transition-colors hover:text-slate-600"
              >
                <X size={24} />
              </button>
            </Dialog.Close>
          </div>
          <Dialog.Description className="sr-only">
            Filter shelters by capacity and minimum rating.
          </Dialog.Description>

          <form onSubmit={onSubmit} className="space-y-6">
            <fieldset>
              <legend className="mb-3 text-sm font-medium text-slate-700">Capacity</legend>
              <div className="flex gap-4">
                <Controller
                  control={control}
                  name="minCapacity"
                  render={({ field }) => (
                    <input
                      type="number"
                      placeholder="Min"
                      min={1}
                      value={field.value ?? ''}
                      onChange={(e) =>
                        field.onChange(e.target.value === '' ? null : Number(e.target.value))
                      }
                      onBlur={field.onBlur}
                      ref={field.ref}
                      className="flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                    />
                  )}
                />
                <Controller
                  control={control}
                  name="maxCapacity"
                  render={({ field }) => (
                    <input
                      type="number"
                      placeholder="Max"
                      min={1}
                      value={field.value ?? ''}
                      onChange={(e) =>
                        field.onChange(e.target.value === '' ? null : Number(e.target.value))
                      }
                      onBlur={field.onBlur}
                      ref={field.ref}
                      className="flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                    />
                  )}
                />
              </div>
              {(errors.minCapacity?.message || errors.maxCapacity?.message) && (
                <p className="mt-1 text-xs text-red-600">
                  {errors.minCapacity?.message ?? errors.maxCapacity?.message}
                </p>
              )}
            </fieldset>

            <fieldset>
              <legend className="mb-3 text-sm font-medium text-slate-700">Minimum Rating</legend>
              <Controller
                control={control}
                name="minRating"
                render={({ field }) => (
                  <div className="space-y-2">
                    <label className="flex cursor-pointer items-center gap-2">
                      <input
                        type="radio"
                        name={field.name}
                        checked={field.value === null}
                        onChange={() => field.onChange(null)}
                        onBlur={field.onBlur}
                        className="h-4 w-4 border-slate-300 text-primary-600 focus:ring-primary-500"
                      />
                      <span className="text-sm text-slate-700">Any rating</span>
                    </label>
                    {[1, 2, 3, 4, 5].map((rating) => (
                      <label key={rating} className="flex cursor-pointer items-center gap-2">
                        <input
                          type="radio"
                          name={field.name}
                          checked={field.value === rating}
                          onChange={() => field.onChange(rating)}
                          onBlur={field.onBlur}
                          className="h-4 w-4 border-slate-300 text-primary-600 focus:ring-primary-500"
                        />
                        <span className="flex items-center gap-1 text-sm text-slate-700">
                          {rating}+ stars
                          <span className="ml-1 flex items-center">
                            {Array.from({ length: rating }).map((_, i) => (
                              <Star
                                key={`f-${i}`}
                                className="h-3.5 w-3.5 fill-current text-accent-400"
                              />
                            ))}
                            {Array.from({ length: 5 - rating }).map((_, i) => (
                              <Star key={`e-${i}`} className="h-3.5 w-3.5 text-slate-300" />
                            ))}
                          </span>
                        </span>
                      </label>
                    ))}
                  </div>
                )}
              />
              {errors.minRating?.message && (
                <p className="mt-1 text-xs text-red-600">{errors.minRating.message}</p>
              )}
            </fieldset>

            <div className="mt-8 flex items-center justify-between border-t border-slate-200 pt-6">
              <button
                type="button"
                onClick={onClear}
                className="px-4 py-2 text-sm font-medium text-slate-700 underline transition-colors hover:text-slate-900"
              >
                Clear all
              </button>
              <button
                type="submit"
                className="rounded-lg bg-primary-600 px-6 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700"
              >
                Apply filters
              </button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
