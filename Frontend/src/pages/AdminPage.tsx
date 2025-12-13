import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../lib/api';
import type { AdminStats } from '../lib/api';
import { useAuth } from '../context/AuthContext';
import '../styles/Admin.css';

const AdminPage: React.FC = () => {
  const { token } = useAuth();
  const { t } = useTranslation();
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    api
      .getAdminStats(token)
      .then(setStats)
      .catch((err) => setError(err.message || 'İstatistik alınamadı'));
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
            <span>Aktif Ürün</span>
            <strong>{stats.activeProducts}</strong>
          </div>
          <div className="stat-card">
            <span>Stok Yok</span>
            <strong>{stats.outOfStockProducts}</strong>
          </div>
          <div className="stat-card">
            <span>Kategori</span>
            <strong>{stats.totalCategories}</strong>
          </div>
          <div className="stat-card">
            <span>{t('orders_title')}</span>
            <strong>{stats.totalOrders}</strong>
          </div>
          <div className="stat-card">
            <span>Bekleyen</span>
            <strong>{stats.pendingOrders}</strong>
          </div>
          <div className="stat-card">
            <span>Tamamlanan</span>
            <strong>{stats.completedOrders}</strong>
          </div>
          <div className="stat-card">
            <span>İptal</span>
            <strong>{stats.cancelledOrders}</strong>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminPage;
