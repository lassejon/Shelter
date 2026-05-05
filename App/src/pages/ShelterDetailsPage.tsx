import { useParams } from 'react-router';

export default function ShelterDetailsPage() {
  const { id } = useParams<{ id: string }>();
  return (
    <div className="p-6">
      <h1 className="text-xl font-semibold text-slate-900">Shelter {id}</h1>
      <p className="mt-2 text-sm text-slate-600">Phase 1 placeholder. Real detail view in Phase 3.</p>
    </div>
  );
}
