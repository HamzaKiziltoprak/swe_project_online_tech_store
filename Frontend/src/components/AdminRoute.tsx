import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const AdminRoute: React.FC = () => {
  const { token, user, loading } = useAuth();

  if (loading) return <div>Loading...</div>;
  if (!token) return <Navigate to="/login" replace />;
  if (!user?.roles?.includes('Admin')) return <Navigate to="/products" replace />;

  return <Outlet />;
};

export default AdminRoute;
