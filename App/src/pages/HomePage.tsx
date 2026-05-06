import { useNavigate } from 'react-router';
import ShelterMap from '@/features/map/components/ShelterMap';

export default function HomePage() {
  const navigate = useNavigate();

  return (
    <div className="h-full w-full">
      <ShelterMap onShelterClick={({ shelter }) => navigate(`/shelters/${shelter.id}`)} />
    </div>
  );
}
