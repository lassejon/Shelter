import { Controller, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form';
import { LocationPicker, type LatLng } from '@/features/shelters/components/LocationPicker';
import {
  BookingApprovalMode,
  ShelterBookingPolicy,
  bookingApprovalModeDescription,
  bookingApprovalModeLabel,
  bookingPolicyDescription,
  bookingPolicyLabel,
} from '@/features/shelters/models/dto';
import type { ShelterFormInput } from '@/features/shelters/models/createShelter.schema';

const POLICY_OPTIONS = [
  ShelterBookingPolicy.Both,
  ShelterBookingPolicy.ExclusiveOnly,
  ShelterBookingPolicy.InclusiveOnly,
];

const APPROVAL_OPTIONS = [BookingApprovalMode.Instant, BookingApprovalMode.RequiresApproval];

interface ShelterCommonFieldsProps {
  register: UseFormRegister<ShelterFormInput>;
  control: Control<ShelterFormInput>;
  errors: FieldErrors<ShelterFormInput>;
  isPending: boolean;
}

/**
 * Basic Info + Booking Policy + Approval Mode + Location. Identical between the Create and
 * Edit shelter pages; promoted here on actual reuse.
 */
export function ShelterCommonFields({
  register,
  control,
  errors,
  isPending,
}: ShelterCommonFieldsProps) {
  return (
    <>
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
                          <div className="font-medium text-slate-900">
                            {bookingPolicyLabel(policy)}
                          </div>
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

          <fieldset>
            <legend className="mb-2 text-sm font-medium text-slate-700">
              Approval Mode <span className="text-red-500">*</span>
            </legend>
            <Controller
              control={control}
              name="bookingApprovalMode"
              render={({ field }) => (
                <div className="space-y-3">
                  {APPROVAL_OPTIONS.map((mode) => {
                    const checked = field.value === mode;
                    return (
                      <label
                        key={mode}
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
                          onChange={() => field.onChange(mode)}
                          onBlur={field.onBlur}
                          disabled={isPending}
                          className="mt-1"
                        />
                        <div>
                          <div className="font-medium text-slate-900">
                            {bookingApprovalModeLabel(mode)}
                          </div>
                          <div className="text-sm text-slate-600">
                            {bookingApprovalModeDescription(mode)}
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
    </>
  );
}
