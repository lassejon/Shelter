import { Link, useParams } from 'react-router';
import { ArrowLeft, Calendar, Loader2, Pencil } from 'lucide-react';
import { useShelter } from '@/features/shelters/hooks/useShelter';
import { useShelterBookings } from '@/features/bookings/hooks/useShelterBookings';
import { Card } from '@/shared/ui/Card';
import { LinkButton } from '@/shared/ui/Button';
import { BookingStatus } from '@/features/bookings/models/dto';

export function ManageShelterHubPage() {
  const { id } = useParams<{ id: string }>();
  const { data: shelter, isLoading, error } = useShelter(id);
  const { data: bookings } = useShelterBookings(id);

  if (isLoading) {
    return (
      <Card className="flex items-center justify-center p-8" padding="none">
        <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
      </Card>
    );
  }
  if (error || !shelter || !id) {
    return (
      <Card className="p-8 text-center" padding="none">
        <p className="font-semibold text-red-600">Shelter not found</p>
      </Card>
    );
  }

  const pendingCount =
    bookings?.filter((b) => Number(b.status) === BookingStatus.Pending).length ?? 0;

  return (
    <div className="space-y-6">
      <div>
        <Link
          to="/settings/shelters"
          className="mb-3 inline-flex items-center gap-2 text-sm font-medium text-primary-600 hover:text-primary-700"
        >
          <ArrowLeft size={18} />
          All shelters
        </Link>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-3xl font-semibold text-slate-900">{shelter.name}</h1>
            {shelter.description && (
              <p className="mt-2 max-w-prose text-slate-600">{shelter.description}</p>
            )}
          </div>
          <span
            className={`rounded-full px-3 py-1 text-sm font-medium ${
              shelter.isActive
                ? 'bg-primary-100 text-primary-800'
                : 'bg-slate-200 text-slate-700'
            }`}
          >
            {shelter.isActive ? 'Active' : 'Deactivated'}
          </span>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <ActionCard
          to={`/settings/shelters/${id}/edit`}
          icon={<Pencil className="h-6 w-6 text-primary-600" />}
          title="Edit details"
          description="Name, description, capacity, location, photos, and booking rules."
        />
        <ActionCard
          to={`/settings/shelters/${id}/bookings`}
          icon={<Calendar className="h-6 w-6 text-primary-600" />}
          title="Manage bookings"
          description={
            pendingCount > 0
              ? `${pendingCount} pending ${pendingCount === 1 ? 'booking' : 'bookings'} awaiting your decision.`
              : 'View, approve, and cancel bookings on this shelter.'
          }
          badge={
            pendingCount > 0 ? (
              <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-800">
                {pendingCount} pending
              </span>
            ) : null
          }
        />
      </div>

      <div className="flex">
        <LinkButton to={`/shelters/${id}`} variant="link" size="inline">
          View public page →
        </LinkButton>
      </div>
    </div>
  );
}

function ActionCard({
  to,
  icon,
  title,
  description,
  badge,
}: {
  to: string;
  icon: React.ReactNode;
  title: string;
  description: string;
  badge?: React.ReactNode;
}) {
  return (
    <Link
      to={to}
      className="group rounded-lg border border-slate-200 bg-white p-6 transition-shadow hover:shadow-md focus:outline-none focus:ring-2 focus:ring-primary-500"
    >
      <div className="flex items-start gap-3">
        <div className="rounded-lg bg-primary-50 p-3">{icon}</div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-2">
            <h3 className="text-lg font-semibold text-slate-900 group-hover:text-primary-700">
              {title}
            </h3>
            {badge}
          </div>
          <p className="mt-1 text-sm text-slate-600">{description}</p>
        </div>
      </div>
    </Link>
  );
}
