import { Link } from 'react-router';
import { ChevronRight, House, Loader2, Plus } from 'lucide-react';
import { useMyShelters } from '@/features/shelters/hooks/useMyShelters';
import { Card } from '@/shared/ui/Card';
import { LinkButton } from '@/shared/ui/Button';
import type { ShelterDetailResponse } from '@/features/shelters/models/dto';

export function ManageSheltersPage() {
  const { data: shelters, isLoading, error } = useMyShelters();

  if (isLoading) {
    return (
      <Card className="flex items-center justify-center p-8" padding="none">
        <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="p-8 text-center" padding="none">
        <p className="font-semibold text-red-600">Failed to load shelters</p>
        <p className="mt-1 text-sm text-slate-600">Please try again later.</p>
      </Card>
    );
  }

  if (!shelters || shelters.length === 0) {
    return (
      <Card className="p-8 text-center" padding="none">
        <House className="mx-auto mb-4 h-12 w-12 text-slate-300" />
        <p className="mb-2 text-slate-600">You don't own any shelters yet.</p>
        <p className="mb-6 text-sm text-slate-500">
          Create your first shelter to start hosting bookings.
        </p>
        <LinkButton to="/shelters/create" variant="primary">
          <Plus size={18} />
          Create shelter
        </LinkButton>
      </Card>
    );
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="mb-2 text-3xl font-semibold text-slate-900">Manage shelters</h1>
          <p className="text-slate-600">Edit details, manage bookings, and toggle availability.</p>
        </div>
        <LinkButton to="/shelters/create" variant="primary">
          <Plus size={18} />
          New shelter
        </LinkButton>
      </div>
      <div className="space-y-4">
        {shelters.map((shelter) => (
          <ShelterRow key={String(shelter.id)} shelter={shelter} />
        ))}
      </div>
    </div>
  );
}

function ShelterRow({ shelter }: { shelter: ShelterDetailResponse }) {
  const cover = [...shelter.pictures].sort(
    (a, b) => Number(a.sortOrder) - Number(b.sortOrder),
  )[0];
  const reviewCount = Number(shelter.reviewSummary.totalCount);
  const average = Number(shelter.reviewSummary.averageRating);

  return (
    <Card as="article" padding="none" variant="interactive">
      <Link
        to={`/settings/shelters/${shelter.id}`}
        className="flex items-stretch gap-4 p-4 focus:outline-none focus:ring-2 focus:ring-primary-500"
      >
        <div className="h-24 w-32 shrink-0 overflow-hidden rounded-md bg-slate-100">
          {cover ? (
            <img src={cover.url} alt={shelter.name} className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-slate-300">
              <House size={32} />
            </div>
          )}
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="truncate text-lg font-semibold text-slate-900">{shelter.name}</h3>
            {!shelter.isActive && (
              <span className="rounded-full bg-slate-200 px-2 py-0.5 text-xs font-medium text-slate-700">
                Deactivated
              </span>
            )}
          </div>
          {shelter.description && (
            <p className="mt-1 line-clamp-2 text-sm text-slate-600">{shelter.description}</p>
          )}
          <p className="mt-1 text-sm text-slate-500">
            Capacity {Number(shelter.capacity)}
            {reviewCount > 0 && (
              <>
                {' '}· {average.toFixed(1)}★ ({reviewCount}{' '}
                {reviewCount === 1 ? 'review' : 'reviews'})
              </>
            )}
          </p>
        </div>
        <ChevronRight className="self-center text-slate-400" />
      </Link>
    </Card>
  );
}
