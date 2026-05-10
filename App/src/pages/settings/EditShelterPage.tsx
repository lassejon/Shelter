import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { ArrowLeft, Loader2, Save, Trash2 } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { ImageUpload } from '@/features/shelters/components/ImageUpload';
import { ShelterCommonFields } from '@/features/shelters/components/ShelterCommonFields';
import { useShelter } from '@/features/shelters/hooks/useShelter';
import { useUpdateShelter } from '@/features/shelters/hooks/useUpdateShelter';
import {
  BookingApprovalMode,
  ShelterBookingPolicy,
  type ShelterDetailResponse,
} from '@/features/shelters/models/dto';
import {
  shelterFormSchema,
  type ShelterFormInput,
} from '@/features/shelters/models/createShelter.schema';

export default function EditShelterPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: shelter, isLoading, error } = useShelter(id);
  const updateMutation = useUpdateShelter();

  const [newPictures, setNewPictures] = useState<File[]>([]);
  const [pictureIdsToDelete, setPictureIdsToDelete] = useState<Set<string>>(new Set());

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ShelterFormInput>({
    resolver: zodResolver(shelterFormSchema),
    defaultValues: {
      name: '',
      description: '',
      capacity: undefined as unknown as number,
      latitude: undefined as unknown as number,
      longitude: undefined as unknown as number,
      bookingPolicy: ShelterBookingPolicy.Both,
      bookingApprovalMode: BookingApprovalMode.Instant,
    },
  });

  // Pre-fill the form once the shelter loads.
  useEffect(() => {
    if (!shelter) return;
    reset({
      name: shelter.name,
      description: shelter.description ?? '',
      capacity: Number(shelter.capacity),
      latitude: Number(shelter.latitude),
      longitude: Number(shelter.longitude),
      bookingPolicy: Number(shelter.bookingPolicy) as ShelterFormInput['bookingPolicy'],
      bookingApprovalMode: Number(
        shelter.bookingApprovalMode,
      ) as ShelterFormInput['bookingApprovalMode'],
    });
  }, [shelter, reset]);

  if (isLoading) {
    return <CenterLoader />;
  }
  if (error || !shelter || !id) {
    return <NotFound />;
  }

  const isPending = isSubmitting || updateMutation.isPending;

  const onSubmit = handleSubmit(async (values) => {
    try {
      await updateMutation.mutateAsync({
        id,
        input: {
          name: values.name,
          description: values.description?.trim() || undefined,
          capacity: values.capacity,
          bookingPolicy: values.bookingPolicy,
          bookingApprovalMode: values.bookingApprovalMode,
        },
        newPictures,
        pictureIdsToDelete: Array.from(pictureIdsToDelete),
      });
      toast.success('Shelter updated');
      navigate(`/settings/shelters/${id}`);
    } catch (err) {
      const detail =
        err instanceof AxiosError && err.response?.data && typeof err.response.data === 'object'
          ? (err.response.data as { detail?: string; title?: string }).detail ??
            (err.response.data as { title?: string }).title
          : null;
      toast.error(detail ?? 'Could not update shelter');
    }
  });

  function togglePictureDeletion(pictureId: string) {
    setPictureIdsToDelete((prev) => {
      const next = new Set(prev);
      if (next.has(pictureId)) next.delete(pictureId);
      else next.add(pictureId);
      return next;
    });
  }

  async function handleDeactivateToggle() {
    if (!shelter || !id) return;
    const turningOff = shelter.isActive;
    if (
      turningOff &&
      !window.confirm(
        'Deactivate this shelter? It will be hidden from public search until you reactivate it.',
      )
    ) {
      return;
    }
    try {
      await updateMutation.mutateAsync({
        id,
        input: {
          name: shelter.name,
          description: shelter.description ?? undefined,
          capacity: Number(shelter.capacity),
          bookingPolicy: Number(shelter.bookingPolicy) as ShelterFormInput['bookingPolicy'],
          bookingApprovalMode: Number(
            shelter.bookingApprovalMode,
          ) as ShelterFormInput['bookingApprovalMode'],
          isActive: !turningOff,
        },
        newPictures: [],
        pictureIdsToDelete: [],
      });
      toast.success(turningOff ? 'Shelter deactivated' : 'Shelter reactivated');
    } catch (err) {
      const detail =
        err instanceof AxiosError && err.response?.data && typeof err.response.data === 'object'
          ? (err.response.data as { detail?: string; title?: string }).detail ??
            (err.response.data as { title?: string }).title
          : null;
      toast.error(detail ?? 'Could not change shelter status');
    }
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      <div className="mb-6">
        <Link
          to={`/settings/shelters/${id}`}
          className="mb-4 inline-flex items-center gap-2 text-sm font-medium text-primary-600 hover:text-primary-700"
        >
          <ArrowLeft size={20} />
          Back to shelter
        </Link>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-3xl font-bold text-slate-900">Edit Shelter</h1>
            <p className="mt-2 text-slate-600">Update the details of your shelter.</p>
          </div>
          <ActiveBadge isActive={shelter.isActive} onToggle={handleDeactivateToggle} disabled={isPending} />
        </div>
      </div>

      <form onSubmit={onSubmit} className="space-y-6">
        <ShelterCommonFields
          register={register}
          control={control}
          errors={errors}
          isPending={isPending}
        />

        <ExistingPicturesSection
          shelter={shelter}
          markedForDeletion={pictureIdsToDelete}
          onToggle={togglePictureDeletion}
          disabled={isPending}
        />

        <section className="rounded-lg bg-white p-6 shadow">
          <h2 className="mb-2 text-xl font-semibold text-slate-900">Add new photos</h2>
          <p className="mb-4 text-sm text-slate-600">
            Upload more photos for this shelter (max 10 per upload).
          </p>
          <ImageUpload value={newPictures} onChange={setNewPictures} />
        </section>

        <div className="flex justify-end gap-3">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate(`/settings/shelters/${id}`)}
            disabled={isPending}
          >
            Cancel
          </Button>
          <Button type="submit" variant="primary" disabled={isPending}>
            <Save size={18} />
            {isPending ? 'Saving…' : 'Save changes'}
          </Button>
        </div>
      </form>
    </div>
  );
}

function ExistingPicturesSection({
  shelter,
  markedForDeletion,
  onToggle,
  disabled,
}: {
  shelter: ShelterDetailResponse;
  markedForDeletion: Set<string>;
  onToggle: (pictureId: string) => void;
  disabled: boolean;
}) {
  if (shelter.pictures.length === 0) {
    return null;
  }

  const sorted = [...shelter.pictures].sort((a, b) => Number(a.sortOrder) - Number(b.sortOrder));
  return (
    <section className="rounded-lg bg-white p-6 shadow">
      <h2 className="mb-2 text-xl font-semibold text-slate-900">Existing photos</h2>
      <p className="mb-4 text-sm text-slate-600">
        Click a photo to mark it for deletion. Changes apply when you save.
      </p>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
        {sorted.map((picture) => {
          const marked = markedForDeletion.has(String(picture.id));
          return (
            <button
              key={String(picture.id)}
              type="button"
              onClick={() => onToggle(String(picture.id))}
              disabled={disabled}
              aria-pressed={marked}
              className={`group relative aspect-square overflow-hidden rounded-lg border-2 transition-all ${
                marked
                  ? 'border-red-500 ring-2 ring-red-500'
                  : 'border-slate-200 hover:border-slate-400'
              }`}
            >
              <img
                src={picture.url}
                alt={picture.caption ?? 'Shelter photo'}
                className={`h-full w-full object-cover transition-opacity ${
                  marked ? 'opacity-40' : 'opacity-100'
                }`}
              />
              {marked && (
                <div className="absolute inset-0 flex items-center justify-center bg-red-500/40">
                  <Trash2 className="h-8 w-8 text-white drop-shadow" />
                </div>
              )}
            </button>
          );
        })}
      </div>
    </section>
  );
}

function ActiveBadge({
  isActive,
  onToggle,
  disabled,
}: {
  isActive: boolean;
  onToggle: () => void;
  disabled: boolean;
}) {
  return (
    <div className="flex items-center gap-3">
      <span
        className={`rounded-full px-3 py-1 text-sm font-medium ${
          isActive ? 'bg-primary-100 text-primary-800' : 'bg-slate-200 text-slate-700'
        }`}
      >
        {isActive ? 'Active' : 'Deactivated'}
      </span>
      <Button type="button" variant="secondary" size="inline" onClick={onToggle} disabled={disabled}>
        {isActive ? 'Deactivate' : 'Reactivate'}
      </Button>
    </div>
  );
}

function CenterLoader() {
  return (
    <Card className="flex items-center justify-center p-8" padding="none">
      <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
    </Card>
  );
}

function NotFound() {
  return (
    <Card className="p-8 text-center" padding="none">
      <p className="font-semibold text-red-600">Shelter not found</p>
      <p className="mt-1 text-sm text-slate-600">
        It may have been deleted or you might not have access.
      </p>
    </Card>
  );
}
