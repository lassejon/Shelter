import { Link, useParams } from 'react-router';
import { AxiosError } from 'axios';
import { ArrowLeft, Calendar, MapPin, Users } from 'lucide-react';
import { useShelter } from '@/features/shelters/hooks/useShelter';
import { PictureGallery } from '@/features/shelters/components/PictureGallery';
import { ReviewSummaryBadge } from '@/features/shelters/components/ReviewSummaryBadge';
import { bookingPolicyLabel } from '@/features/shelters/models/dto';

export default function ShelterDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: shelter, isLoading, error } = useShelter(id);

  if (isLoading) return <Layout><LoadingSkeleton /></Layout>;
  if (error) return <Layout><ErrorState error={error} /></Layout>;
  if (!shelter) return null;

  const capacity = Number(shelter.capacity);
  const lat = Number(shelter.latitude);
  const lng = Number(shelter.longitude);

  return (
    <Layout>
      <BackLink />

      <div className="mb-6">
        <PictureGallery pictures={shelter.pictures} shelterName={shelter.name} />
      </div>

      <div className="mb-6 rounded-lg bg-white p-6 shadow">
        <h1 className="mb-2 text-3xl font-bold text-slate-900">{shelter.name}</h1>

        <div className="mb-4">
          <ReviewSummaryBadge summary={shelter.reviewSummary} />
        </div>

        {shelter.description && (
          <div className="mb-6">
            <h2 className="mb-2 text-lg font-semibold text-slate-900">About</h2>
            <p className="whitespace-pre-wrap text-slate-700">{shelter.description}</p>
          </div>
        )}

        <div className="grid grid-cols-1 gap-4 border-t border-slate-200 pt-6 md:grid-cols-2">
          <DetailItem icon={<Users className="h-5 w-5 text-primary-600" />} label="Capacity">
            {capacity} guests
          </DetailItem>
          <DetailItem icon={<Calendar className="h-5 w-5 text-primary-600" />} label="Booking Policy">
            {bookingPolicyLabel(Number(shelter.bookingPolicy))}
          </DetailItem>
          <DetailItem icon={<MapPin className="h-5 w-5 text-primary-600" />} label="Location">
            <span className="text-sm">{lat.toFixed(6)}, {lng.toFixed(6)}</span>
          </DetailItem>
          <DetailItem icon={<StatusDot active={shelter.isActive} />} label="Status">
            {shelter.isActive ? 'Active' : 'Inactive'}
          </DetailItem>
        </div>

        <div className="mt-6 border-t border-slate-200 pt-4">
          <div className="grid grid-cols-1 gap-2 text-sm text-slate-500 md:grid-cols-2">
            <div>Created: {new Date(shelter.createdAt).toLocaleDateString()}</div>
            <div>Updated: {new Date(shelter.updatedAt).toLocaleDateString()}</div>
          </div>
        </div>
      </div>
    </Layout>
  );
}

function Layout({ children }: { children: React.ReactNode }) {
  return <main className="mx-auto max-w-4xl px-4 py-8">{children}</main>;
}

function BackLink() {
  return (
    <Link
      to="/"
      className="mb-6 inline-flex items-center gap-2 text-primary-600 transition-colors hover:text-primary-700"
    >
      <ArrowLeft className="h-4 w-4" />
      Back to Map
    </Link>
  );
}

function DetailItem({
  icon,
  label,
  children,
}: {
  icon: React.ReactNode;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex items-start gap-3">
      <div className="mt-0.5">{icon}</div>
      <div>
        <div className="font-medium text-slate-900">{label}</div>
        <div className="text-slate-600">{children}</div>
      </div>
    </div>
  );
}

function StatusDot({ active }: { active: boolean }) {
  return (
    <div className="mt-0.5 flex h-5 w-5 items-center justify-center">
      <div
        className={`h-3 w-3 rounded-full ${active ? 'bg-primary-500' : 'bg-slate-400'}`}
      />
    </div>
  );
}

function LoadingSkeleton() {
  return (
    <div className="animate-pulse">
      <div className="mb-4 h-8 w-1/4 rounded bg-slate-200" />
      <div className="mb-6 grid grid-cols-1 gap-4 md:grid-cols-3">
        <div className="h-64 rounded bg-slate-200 md:col-span-2" />
        <div className="h-64 rounded bg-slate-200" />
      </div>
      <div className="rounded-lg bg-white p-6 shadow">
        <div className="mb-4 h-6 w-3/4 rounded bg-slate-200" />
        <div className="mb-2 h-4 w-full rounded bg-slate-200" />
        <div className="h-4 w-5/6 rounded bg-slate-200" />
      </div>
    </div>
  );
}

function ErrorState({ error }: { error: unknown }) {
  const isNotFound = error instanceof AxiosError && error.response?.status === 404;
  return (
    <div className="rounded-lg bg-white p-6 text-center shadow">
      <h1 className="mb-4 text-2xl font-bold text-slate-900">
        {isNotFound ? 'Shelter Not Found' : 'Error Loading Shelter'}
      </h1>
      <p className="mb-6 text-slate-600">
        {isNotFound
          ? "The shelter you're looking for doesn't exist or has been removed."
          : error instanceof Error
            ? error.message
            : 'Failed to load shelter details'}
      </p>
      <Link
        to="/"
        className="inline-flex items-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to Map
      </Link>
    </div>
  );
}
