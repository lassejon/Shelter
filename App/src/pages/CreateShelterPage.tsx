import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { ArrowLeft, Save } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { LocationPicker, type LatLng } from '@/features/shelters/components/LocationPicker';
import { ImageUpload } from '@/features/shelters/components/ImageUpload';
import { useCreateShelter } from '@/features/shelters/hooks/useCreateShelter';
import {
  ShelterBookingPolicy,
  bookingPolicyDescription,
  bookingPolicyLabel,
} from '@/features/shelters/models/dto';
import {
  createShelterSchema,
  type CreateShelterInput,
} from '@/features/shelters/models/createShelter.schema';

const POLICY_OPTIONS = [
  ShelterBookingPolicy.Both,
  ShelterBookingPolicy.ExclusiveOnly,
  ShelterBookingPolicy.InclusiveOnly,
];

export default function CreateShelterPage() {
  const navigate = useNavigate();
  const createMutation = useCreateShelter();
  const [pictures, setPictures] = useState<File[]>([]);

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CreateShelterInput>({
    resolver: zodResolver(createShelterSchema),
    defaultValues: {
      name: '',
      description: '',
      capacity: undefined as unknown as number,
      latitude: undefined as unknown as number,
      longitude: undefined as unknown as number,
      bookingPolicy: ShelterBookingPolicy.Both,
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
        <section className="rounded-lg bg-white p-6 shadow">
          <h2 className="mb-4 text-xl font-semibold text-slate-900">Basic Information</h2>

          <div className="space-y-4">
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">
                Shelter Name <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                placeholder="e.g., Lakeside Cabin, Forest Retreat"
                disabled={isPending}
                aria-invalid={Boolean(errors.name)}
                className={`w-full rounded-lg border px-4 py-2 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 ${
                  errors.name ? 'border-red-500' : 'border-slate-300'
                }`}
                {...register('name')}
              />
              {errors.name?.message && (
                <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>
              )}
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">Description</label>
              <textarea
                rows={4}
                placeholder="Describe your shelter, amenities, and what makes it special..."
                disabled={isPending}
                className="w-full resize-none rounded-lg border border-slate-300 px-4 py-2 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500"
                {...register('description')}
              />
              <p className="mt-1 text-sm text-slate-500">
                Optional — helps potential guests find and pick your shelter.
              </p>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">
                Capacity <span className="text-red-500">*</span>
              </label>
              <Controller
                control={control}
                name="capacity"
                render={({ field }) => (
                  <input
                    type="number"
                    placeholder="Maximum number of guests"
                    min={1}
                    max={100}
                    disabled={isPending}
                    aria-invalid={Boolean(errors.capacity)}
                    value={field.value ?? ''}
                    onChange={(e) =>
                      field.onChange(
                        e.target.value === '' ? undefined : Number.parseInt(e.target.value, 10),
                      )
                    }
                    onBlur={field.onBlur}
                    ref={field.ref}
                    className={`w-full rounded-lg border px-4 py-2 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 ${
                      errors.capacity ? 'border-red-500' : 'border-slate-300'
                    }`}
                  />
                )}
              />
              {errors.capacity?.message && (
                <p className="mt-1 text-sm text-red-600">{errors.capacity.message}</p>
              )}
            </div>

            <fieldset>
              <legend className="mb-2 text-sm font-medium text-slate-700">
                Booking Policy <span className="text-red-500">*</span>
              </legend>
              <Controller
                control={control}
                name="bookingPolicy"
                render={({ field }) => (
                  <div className="space-y-3">
                    {POLICY_OPTIONS.map((policy) => {
                      const checked = field.value === policy;
                      return (
                        <label
                          key={policy}
                          className={`flex cursor-pointer items-start gap-3 rounded-lg border p-4 transition-colors ${
                            checked
                              ? 'border-primary-500 bg-primary-50'
                              : 'border-slate-300 hover:border-slate-400'
                          }`}
                        >
                          <input
                            type="radio"
                            name={field.name}
                            checked={checked}
                            onChange={() => field.onChange(policy)}
                            onBlur={field.onBlur}
                            disabled={isPending}
                            className="mt-1"
                          />
                          <div>
                            <div className="font-medium text-slate-900">{bookingPolicyLabel(policy)}</div>
                            <div className="text-sm text-slate-600">
                              {bookingPolicyDescription(policy)}
                            </div>
                          </div>
                        </label>
                      );
                    })}
                  </div>
                )}
              />
            </fieldset>
          </div>
        </section>

        <section className="rounded-lg bg-white p-6 shadow">
          <h2 className="mb-4 text-xl font-semibold text-slate-900">
            Location <span className="text-red-500">*</span>
          </h2>
          <Controller
            control={control}
            name="latitude"
            render={({ field: latField }) => (
              <Controller
                control={control}
                name="longitude"
                render={({ field: lngField }) => {
                  const value: LatLng | null =
                    typeof latField.value === 'number' && typeof lngField.value === 'number'
                      ? { latitude: latField.value, longitude: lngField.value }
                      : null;
                  return (
                    <LocationPicker
                      value={value}
                      onChange={(next) => {
                        latField.onChange(next.latitude);
                        lngField.onChange(next.longitude);
                      }}
                      onBlur={() => {
                        latField.onBlur();
                        lngField.onBlur();
                      }}
                    />
                  );
                }}
              />
            )}
          />
          {(errors.latitude?.message || errors.longitude?.message) && (
            <p className="mt-2 text-sm text-red-600">
              {errors.latitude?.message ?? errors.longitude?.message}
            </p>
          )}
        </section>

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
