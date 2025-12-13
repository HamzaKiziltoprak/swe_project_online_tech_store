import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

interface ProtectedRouteProps {
  allowAnonymous?: boolean;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ allowAnonymous = false }) => {
  const { token, loading } = useAuth();

  if (loading) {
    return <div>Oturum kontrol ediliyor...</div>;
  }

  if (!token && !allowAnonymous) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
};

export default ProtectedRoute;
