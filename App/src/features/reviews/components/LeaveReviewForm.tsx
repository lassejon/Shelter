import { useEffect, useMemo, useRef, useState } from 'react';
import { Camera, X } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { useCreateReview } from '@/features/reviews/hooks/useCreateReview';
import { useUpdateReview } from '@/features/reviews/hooks/useUpdateReview';
import {
  createReviewSchema,
  type CreateReviewInput,
} from '@/features/reviews/models/createReview.schema';
import type {
  Rating,
  ReviewDetailResponse,
  ReviewPictureResponse,
} from '@/features/reviews/models/dto';
import { StarRating } from './StarRating';

interface LeaveReviewFormProps {
  shelterId: string;
  /** When set, the form is in edit mode (load values, call updateReview, optionally remove existing pictures). */
  editingReview?: ReviewDetailResponse;
  onCancel?: () => void;
  onSuccess?: () => void;
}

const MAX_PICTURES = 5;

export function LeaveReviewForm({
  shelterId,
  editingReview,
  onCancel,
  onSuccess,
}: LeaveReviewFormProps) {
  const isEditMode = Boolean(editingReview);

  const createMutation = useCreateReview();
  const updateMutation = useUpdateReview();
  const queryClient = useQueryClient();

  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateReviewInput>({
    resolver: zodResolver(createReviewSchema),
    defaultValues: {
      rating: (editingReview ? (Number(editingReview.rating) as Rating) : undefined) as
        | Rating
        | undefined,
      comment: editingReview?.comment ?? '',
    },
  });

  const [newPictures, setNewPictures] = useState<File[]>([]);
  const [existingPictures, setExistingPictures] = useState<ReviewPictureResponse[]>(
    editingReview?.pictures ?? [],
  );
  const [pictureIdsToDelete, setPictureIdsToDelete] = useState<string[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const newPreviews = useMemo(
    () => newPictures.map((f) => URL.createObjectURL(f)),
    [newPictures],
  );
  useEffect(() => () => newPreviews.forEach((url) => URL.revokeObjectURL(url)), [newPreviews]);

  const totalPictures = existingPictures.length + newPictures.length;

  function addFiles(files: FileList | null) {
    if (!files) return;
    const images = Array.from(files).filter((f) => f.type.startsWith('image/'));
    const room = Math.max(0, MAX_PICTURES - totalPictures);
    setNewPictures((prev) => [...prev, ...images.slice(0, room)]);
  }

  function removeNew(index: number) {
    setNewPictures((prev) => prev.filter((_, i) => i !== index));
  }

  function removeExisting(pictureId: string) {
    setExistingPictures((prev) => prev.filter((p) => p.id !== pictureId));
    setPictureIdsToDelete((prev) => [...prev, pictureId]);
  }

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (isEditMode && editingReview) {
        await updateMutation.mutateAsync({
          reviewId: editingReview.id,
          shelterId,
          rating: values.rating,
          comment: values.comment ?? '',
          newPictures,
          pictureIdsToDelete,
        });
        toast.success('Review updated');
      } else {
        await createMutation.mutateAsync({
          shelterId,
          input: { rating: values.rating, comment: values.comment },
          pictures: newPictures,
        });
        toast.success('Review posted');
      }
      onSuccess?.();
    } catch (error) {
      if (
        !isEditMode &&
        error instanceof AxiosError &&
        error.response?.status === 400 &&
        /already reviewed/i.test(
          (error.response.data as { detail?: string } | undefined)?.detail ?? '',
        )
      ) {
        // Race: the user already has a review for this shelter (e.g. another tab posted first).
        // Refresh the "mine" query — the parent will swap this form for the existing review card,
        // from which the user can hit Edit to modify it.
        queryClient.invalidateQueries({ queryKey: ['reviews', 'mine', shelterId] });
        toast.info('You already reviewed this shelter — loading your existing review.');
        return;
      }
      toast.error(isEditMode ? 'Could not update review' : 'Could not post review');
    }
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <form onSubmit={onSubmit} className="space-y-4 rounded-lg border border-slate-200 p-5">
      <h3 className="text-lg font-semibold text-slate-900">
        {isEditMode ? 'Edit your review' : 'Leave a review'}
      </h3>

      <div>
        <label className="mb-2 block text-sm font-medium text-slate-700">
          Rating <span className="text-red-500">*</span>
        </label>
        <Controller
          control={control}
          name="rating"
          render={({ field }) => (
            <StarRating
              value={(field.value as Rating | undefined) ?? null}
              onChange={(r) => field.onChange(r)}
              disabled={isPending}
            />
          )}
        />
        {errors.rating && (
          <p className="mt-1 text-sm text-red-600">Please pick a rating.</p>
        )}
      </div>

      <div>
        <label className="mb-2 block text-sm font-medium text-slate-700">Comment</label>
        <textarea
          rows={4}
          placeholder="Share your experience…"
          disabled={isPending}
          className="w-full resize-none rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500"
          {...register('comment')}
        />
        {errors.comment?.message && (
          <p className="mt-1 text-sm text-red-600">{errors.comment.message}</p>
        )}
      </div>

      <div>
        <label className="mb-2 block text-sm font-medium text-slate-700">
          Photos (optional, max {MAX_PICTURES})
        </label>
        <input
          ref={fileInputRef}
          type="file"
          accept="image/*"
          multiple
          className="hidden"
          onChange={(e) => addFiles(e.target.files)}
        />
        <div className="flex flex-wrap items-start gap-2">
          {existingPictures.map((picture) => (
            <PicturePreview
              key={picture.id}
              src={picture.url}
              onRemove={() => removeExisting(picture.id)}
            />
          ))}
          {newPreviews.map((src, index) => (
            <PicturePreview
              key={`new-${index}`}
              src={src}
              onRemove={() => removeNew(index)}
            />
          ))}
          {totalPictures < MAX_PICTURES && (
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              className="flex h-24 w-24 flex-col items-center justify-center rounded-lg border-2 border-dashed border-slate-300 text-slate-500 transition-colors hover:border-primary-400 hover:text-primary-600"
            >
              <Camera className="mb-1 h-5 w-5" />
              <span className="text-[10px] font-medium">Add photo</span>
            </button>
          )}
        </div>
      </div>

      <div className="flex flex-wrap items-center justify-end gap-2 border-t border-slate-200 pt-4">
        {isEditMode && onCancel && (
          <Button type="button" variant="secondary" onClick={onCancel} disabled={isPending}>
            Cancel
          </Button>
        )}
        <Button type="submit" variant="primary" disabled={isPending}>
          {isPending
            ? isEditMode
              ? 'Updating…'
              : 'Posting…'
            : isEditMode
              ? 'Save changes'
              : 'Post review'}
        </Button>
      </div>
    </form>
  );
}

function PicturePreview({ src, onRemove }: { src: string; onRemove: () => void }) {
  return (
    <div className="group relative">
      <div className="h-24 w-24 overflow-hidden rounded-lg border border-slate-200">
        <img src={src} alt="" className="h-full w-full object-cover" />
      </div>
      <button
        type="button"
        onClick={(e) => {
          e.stopPropagation();
          onRemove();
        }}
        aria-label="Remove photo"
        className="absolute right-1 top-1 rounded-full bg-red-500 p-1 text-white opacity-0 transition-opacity hover:bg-red-600 group-hover:opacity-100"
      >
        <X size={14} />
      </button>
    </div>
  );
}
