import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../lib/api';
import type { AdminStats } from '../lib/api';
import { useAuth } from '../context/AuthContext';
import UserManagement from '../components/admin/UserManagement';
import ProductManagement from '../components/admin/ProductManagement';
import ReviewManagement from '../components/admin/ReviewManagement';
import '../styles/Admin.css';

const AdminPage: React.FC = () => {
  const { token } = useAuth();
  const { t } = useTranslation();
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'users' | 'products' | 'reviews'>('users');

  useEffect(() => {
    if (!token) return;
    api
      .getAdminStats(token)
      .then(setStats)
      .catch((err) => setError(err.message || t('admin_stats_error')));
  }, [token]);

  return (
    <div className="admin-page">
      <h2>{t('admin_title')}</h2>
      <p>{t('stats_title')}</p>
      {error && <p className="error">{error}</p>}
      {!error && !stats && <p>{t('loading')}</p>}
      {stats && (
        <div className="stats-grid">
          <div className="stat-card">
            <span>{t('products')}</span>
            <strong>{stats.totalProducts}</strong>
          </div>
          <div className="stat-card">
            <span>{t('active_products')}</span>
            <strong>{stats.activeProducts}</strong>
          </div>
          <div className="stat-card">
            <span>{t('out_of_stock')}</span>
            <strong>{stats.outOfStockProducts}</strong>
          </div>
          <div className="stat-card">
            <span>{t('categories')}</span>
            <strong>{stats.totalCategories}</strong>
          </div>
          <div className="stat-card">
            <span>{t('orders_title')}</span>
            <strong>{stats.totalOrders}</strong>
          </div>
          <div className="stat-card">
            <span>{t('pending_orders')}</span>
            <strong>{stats.pendingOrders}</strong>
          </div>
          <div className="stat-card">
            <span>{t('completed_orders')}</span>
            <strong>{stats.completedOrders}</strong>
          </div>
          <div className="stat-card">
            <span>{t('cancelled_orders')}</span>
            <strong>{stats.cancelledOrders}</strong>
          </div>
        </div>
      )}

      <div className="admin-tabs">
        <button
          className={`tab-button ${activeTab === 'users' ? 'active' : ''}`}
          onClick={() => setActiveTab('users')}
        >
          👥 {t('user_management_title')}
        </button>
        <button
          className={`tab-button ${activeTab === 'products' ? 'active' : ''}`}
          onClick={() => setActiveTab('products')}
        >
          📦 {t('product_management')}
        </button>
        <button
          className={`tab-button ${activeTab === 'reviews' ? 'active' : ''}`}
          onClick={() => setActiveTab('reviews')}
        >
          💬 {t('review_management')}
        </button>
      </div>

      {activeTab === 'users' && <UserManagement token={token} />}
      {activeTab === 'products' && <ProductManagement token={token} />}
      {activeTab === 'reviews' && <ReviewManagement token={token} />}
    </div>
  );
};

export default AdminPage;
