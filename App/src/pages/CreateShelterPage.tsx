import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { ArrowLeft, Save } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { ImageUpload } from '@/features/shelters/components/ImageUpload';
import { ShelterCommonFields } from '@/features/shelters/components/ShelterCommonFields';
import { useCreateShelter } from '@/features/shelters/hooks/useCreateShelter';
import {
  BookingApprovalMode,
  ShelterBookingPolicy,
} from '@/features/shelters/models/dto';
import {
  shelterFormSchema,
  type ShelterFormInput,
} from '@/features/shelters/models/createShelter.schema';

export default function CreateShelterPage() {
  const navigate = useNavigate();
  const createMutation = useCreateShelter();
  const [pictures, setPictures] = useState<File[]>([]);

  const {
    register,
    handleSubmit,
    control,
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

  const onSubmit = handleSubmit(async (values) => {
    try {
      const created = await createMutation.mutateAsync({
        input: {
          ...values,
          description: values.description?.trim() || undefined,
        },
        pictures,
      });
      toast.success('Shelter created');
      navigate(`/shelters/${created.id}`);
    } catch (error) {
      const detail =
        error instanceof AxiosError && error.response?.data && typeof error.response.data === 'object'
          ? (error.response.data as { detail?: string; title?: string }).detail ??
            (error.response.data as { title?: string }).title
          : null;
      toast.error(detail ?? 'Could not create shelter');
    }
  });

  const isPending = isSubmitting || createMutation.isPending;

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      <div className="mb-6">
        <Link
          to="/"
          className="mb-4 inline-flex items-center gap-2 text-sm font-medium text-primary-600 hover:text-primary-700"
        >
          <ArrowLeft size={20} />
          Back to Map
        </Link>
        <h1 className="text-3xl font-bold text-slate-900">Create New Shelter</h1>
        <p className="mt-2 text-slate-600">Fill in the details below to list your outdoor shelter.</p>
      </div>

      <form onSubmit={onSubmit} className="space-y-6">
        <ShelterCommonFields
          register={register}
          control={control}
          errors={errors}
          isPending={isPending}
        />

        <section className="rounded-lg bg-white p-6 shadow">
          <h2 className="mb-2 text-xl font-semibold text-slate-900">Photos</h2>
          <p className="mb-4 text-sm text-slate-600">
            Add photos to showcase your shelter (optional, max 10 images).
          </p>
          <ImageUpload value={pictures} onChange={setPictures} />
        </section>

        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={() => navigate('/')} disabled={isPending}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" disabled={isPending}>
            <Save size={18} />
            {isPending ? 'Creating…' : 'Create Shelter'}
          </Button>
        </div>
      </form>
    </div>
  );
}
