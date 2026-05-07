import { LogOut, Shield, Star } from 'lucide-react';
import { toast } from 'sonner';
import { useAuthStore } from '@/features/auth/stores/auth.store';
import { useUpgradeToOwner } from '@/features/auth/hooks/useUpgradeToOwner';
import { useLogout } from '@/features/auth/hooks/useLogout';

const SHELTER_OWNER_ROLE = 'ShelterOwner';

export function AccountSettingsPage() {
  const firstName = useAuthStore((s) => s.firstName);
  const lastName = useAuthStore((s) => s.lastName);
  const email = useAuthStore((s) => s.email);
  const roles = useAuthStore((s) => s.roles);

  const upgradeMutation = useUpgradeToOwner();
  const logoutMutation = useLogout();

  const fullName = [firstName, lastName].filter(Boolean).join(' ') || '—';
  const isOwner = roles.includes(SHELTER_OWNER_ROLE);

  function handleUpgrade() {
    upgradeMutation.mutate(undefined, {
      onSuccess: () => toast.success("You're now a shelter owner"),
      onError: () => toast.error('Could not upgrade account'),
    });
  }

  function handleLogout() {
    logoutMutation.mutate();
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="mb-2 text-3xl font-semibold text-slate-900">Account</h1>
        <p className="text-slate-600">Your profile and preferences.</p>
      </div>

      <section className="rounded-lg border border-slate-200 p-6">
        <h2 className="mb-4 text-lg font-semibold text-slate-900">Profile</h2>
        <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <ProfileField label="Name" value={fullName} />
          <ProfileField label="Email" value={email ?? '—'} />
          <div className="sm:col-span-2">
            <dt className="text-sm font-medium text-slate-500">Roles</dt>
            <dd className="mt-2 flex flex-wrap gap-2">
              {roles.length === 0 ? (
                <span className="text-slate-400">—</span>
              ) : (
                roles.map((role) => <RoleBadge key={role} role={role} />)
              )}
            </dd>
          </div>
        </dl>
      </section>

      {!isOwner && (
        <section className="rounded-lg border-2 border-primary-200 bg-primary-50 p-6">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-primary-100">
              <Star className="h-6 w-6 text-primary-700" />
            </div>
            <div className="flex-1">
              <h2 className="text-lg font-semibold text-slate-900">
                Become a shelter owner
              </h2>
              <p className="mt-1 text-sm text-slate-700">
                List your own shelters, manage bookings, and reach travellers looking
                for somewhere to stay.
              </p>
              <button
                type="button"
                onClick={handleUpgrade}
                disabled={upgradeMutation.isPending}
                className="mt-4 inline-flex items-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {upgradeMutation.isPending ? 'Upgrading…' : 'Upgrade my account'}
              </button>
            </div>
          </div>
        </section>
      )}

      <section className="rounded-lg border border-slate-200 p-6">
        <h2 className="mb-2 text-lg font-semibold text-slate-900">Sign out</h2>
        <p className="mb-4 text-sm text-slate-600">End your session on this device.</p>
        <button
          type="button"
          onClick={handleLogout}
          disabled={logoutMutation.isPending}
          className="inline-flex items-center gap-2 rounded-md border border-red-300 bg-white px-4 py-2 text-sm font-medium text-red-700 transition-colors hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
        >
          <LogOut className="h-4 w-4" />
          {logoutMutation.isPending ? 'Signing out…' : 'Sign out'}
        </button>
      </section>
    </div>
  );
}

function ProfileField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-sm font-medium text-slate-500">{label}</dt>
      <dd className="mt-1 text-slate-900">{value}</dd>
    </div>
  );
}

function RoleBadge({ role }: { role: string }) {
  const isOwner = role === SHELTER_OWNER_ROLE;
  const styles = isOwner
    ? 'bg-primary-100 text-primary-800'
    : 'bg-slate-100 text-slate-700';
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-3 py-1 text-xs font-medium ${styles}`}
    >
      {isOwner && <Shield className="h-3 w-3" />}
      {role}
    </span>
  );
}
