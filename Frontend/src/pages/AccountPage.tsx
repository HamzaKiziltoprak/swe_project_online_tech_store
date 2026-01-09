import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import type { FavoriteItem, Order, MyReview } from '../lib/api';
import { useAuth } from '../context/AuthContext';
import '../styles/Account.css';

const AccountPage = () => {
  const { user, token, refreshProfile } = useAuth();
  const { t } = useTranslation();
  const [favorites, setFavorites] = useState<FavoriteItem[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [myReviews, setMyReviews] = useState<MyReview[]>([]);
  const [loadingFavorites, setLoadingFavorites] = useState<boolean>(true);
  const [loadingOrders, setLoadingOrders] = useState<boolean>(true);
  const [loadingReviews, setLoadingReviews] = useState<boolean>(true);
  const isAdmin = user?.roles?.includes('Admin');
  const isProductManager = user?.roles?.includes('ProductManager');
  const isCompanyOwner = user?.roles?.includes('CompanyOwner');

  // Profile Update State
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [address, setAddress] = useState('');
  const [profileMessage, setProfileMessage] = useState<string | null>(null);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [editingProfile, setEditingProfile] = useState(false);

  // Password Change State
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordMessage, setPasswordMessage] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [editingPassword, setEditingPassword] = useState(false);

  useEffect(() => {
    if (!token) return;
    refreshProfile();
    if (!isAdmin && !isProductManager && !isCompanyOwner) {
      api
        .getFavorites(token)
        .then((res) => setFavorites(res.items))
        .finally(() => setLoadingFavorites(false));
      api
        .getOrders(token)
        .then((res) => setOrders(res.items))
        .finally(() => setLoadingOrders(false));
      api
        .getMyReviews(token)
        .then((res) => setMyReviews(res))
        .finally(() => setLoadingReviews(false));
    }
  }, [token, isAdmin, isProductManager, isCompanyOwner, refreshProfile]);

  useEffect(() => {
    if (user) {
      setFirstName(user.firstName);
      setLastName(user.lastName);
      setAddress(user.address || '');
    }
  }, [user]);

  const handleProfileUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    setProfileMessage(null);
    setProfileError(null);
    if (!token) return;
    try {
      await api.updateProfile({ firstName, lastName, address }, token);
      await refreshProfile();
      setProfileMessage(t('profile_update_success'));
      setEditingProfile(false);
    } catch (err: any) {
      setProfileError(err.message || t('profile_update_error'));
    }
  };

  const handlePasswordChange = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordMessage(null);
    setPasswordError(null);
    if (newPassword !== confirmPassword) {
      setPasswordError(t('passwords_do_not_match'));
      return;
    }
    if (!token) return;
    try {
      await api.changePassword({ currentPassword, newPassword, confirmPassword }, token);
      setPasswordMessage(t('password_change_success'));
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setEditingPassword(false);
    } catch (err: any) {
      setPasswordError(err.message || t('password_change_error'));
    }
  };

  return (
    <div className="account-page">
      <section className="panel">
        <div className="panel-header">
          <h3>👤 {t('profile_title')}</h3>
          <button
            className="edit-button"
            onClick={() => setEditingProfile(!editingProfile)}
            title={editingProfile ? t('close') : t('edit')}
          >
            ✏️
          </button>
        </div>

        {editingProfile ? (
          <form onSubmit={handleProfileUpdate} className="profile-form">
            <div className="form-group">
              <label htmlFor="firstName">{t('first_name')}</label>
              <input
                type="text"
                id="firstName"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="lastName">{t('last_name')}</label>
              <input
                type="text"
                id="lastName"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="address">{t('address')}</label>
              <textarea
                id="address"
                value={address}
                onChange={(e) => setAddress(e.target.value)}
              />
            </div>
            <div className="form-actions">
              <button type="submit" className="button">
                ✅ {t('update_profile')}
              </button>
              <button type="button" className="button cancel" onClick={() => setEditingProfile(false)}>
                ❌ {t('cancel')}
              </button>
            </div>
            {profileMessage && <p className="success-message">{profileMessage}</p>}
            {profileError && <p className="error-message">{profileError}</p>}
          </form>
        ) : (
          <div className="profile-view">
            <p>
              <strong>👤 {t('first_name')}:</strong> {user?.firstName}
            </p>
            <p>
              <strong>👤 {t('last_name')}:</strong> {user?.lastName}
            </p>
            <p>
              <strong>✉️ {t('email')}:</strong> {user?.email}
            </p>
            <p>
              <strong>📍 {t('address')}:</strong> {user?.address || '-'}
            </p>
          </div>
        )}
      </section>

      <section className="panel">
        <div className="panel-header">
          <h3>🔒 {t('change_password_title')}</h3>
          <button
            className="edit-button"
            onClick={() => setEditingPassword(!editingPassword)}
            title={editingPassword ? t('close') : t('edit')}
          >
            ✏️
          </button>
        </div>

        {editingPassword ? (
          <form onSubmit={handlePasswordChange} className="password-form">
            <div className="form-group">
              <label htmlFor="currentPassword">{t('current_password')}</label>
              <input
                type="password"
                id="currentPassword"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="newPassword">{t('new_password')}</label>
              <input
                type="password"
                id="newPassword"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="confirmPassword">{t('confirm_password')}</label>
              <input
                type="password"
                id="confirmPassword"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
              />
            </div>
            <div className="form-actions">
              <button type="submit" className="button">
                🔄 {t('change_password')}
              </button>
              <button type="button" className="button cancel" onClick={() => setEditingPassword(false)}>
                ❌ {t('cancel')}
              </button>
            </div>
            {passwordMessage && <p className="success-message">{passwordMessage}</p>}
            {passwordError && <p className="error-message">{passwordError}</p>}
          </form>
        ) : (
          <p className="section-placeholder">{t('click_edit_to_change')}</p>
        )}
      </section>

      {!isAdmin && !isProductManager && !isCompanyOwner && (
        <>
          <section className="panel">
            <h3>⭐ {t('favorites_title')}</h3>
            {loadingFavorites && <p>{t('loading')}</p>}
            {!loadingFavorites && !favorites.length && <p>✨ {t('favorites_empty')}</p>}
            <div className="favorites-grid">
              {favorites.map((fav) => (
                <div key={fav.favoriteID} className="favorite-card">
                  <p className="name">💝 {fav.productName}</p>
                  <p className="price">💰 ₺{fav.price}</p>
                  <span className="badge">🏷️ {fav.brand}</span>
                </div>
              ))}
            </div>
          </section>

          <section className="panel">
            <h3>📦 {t('orders_title')}</h3>
            {loadingOrders && <p>{t('loading')}</p>}
            {!loadingOrders && !orders.length && <p>✨ {t('orders_empty')}</p>}
            <div className="orders-list">
              {orders.map((order) => {
                const statusKey = `order_status_${order.status.toLowerCase()}`;
                const translatedStatus = t(statusKey) || order.status;
                const statusClass = order.status.toLowerCase();
                return (
                  <div key={order.orderID} className="order-card">
                    <div className="order-top">
                      <strong>🛒 #{order.orderID}</strong>
                      <span className={`badge status-${statusClass}`}>📍 {translatedStatus}</span>
                    </div>
                    <p>📅 {new Date(order.orderDate).toLocaleDateString()}</p>
                    <p>
                      💳 {t('total_price')}: ₺{order.totalAmount}
                    </p>
                    <ul>
                      {order.items.map((item) => (
                        <li key={item.orderItemID}>
                          📝 {item.productName} x{item.quantity} - ₺{item.unitPrice}
                        </li>
                      ))}
                    </ul>
                  </div>
                );
              })}
            </div>
          </section>

          <section className="panel">
            <h3>💬 {t('my_reviews_title') || 'My Reviews'}</h3>
            {loadingReviews && <p>{t('loading')}</p>}
            {!loadingReviews && !myReviews.length && <p>✨ {t('no_reviews') || 'You have not written any reviews yet.'}</p>}
            <div className="reviews-list">
              {myReviews.map((review) => (
                <div key={review.productReviewID} className="review-card">
                  <div className="review-top">
                    <Link to={`/products/${review.productID}`} className="product-link">
                      🛍️ {review.productName}
                    </Link>
                    <span className={`status-badge ${review.isApproved ? 'badge-success' : 'badge-warning'}`}>
                      {review.isApproved ? `🟢 ${t('review_published') || 'Published'}` : `🟡 ${t('review_pending') || 'Pending Approval'}`}
                    </span>
                  </div>
                  <div className="review-rating">
                    {'⭐'.repeat(review.rating)}
                  </div>
                  <p className="review-text">{review.reviewText}</p>
                  <p className="review-date">📅 {new Date(review.reviewDate).toLocaleDateString()}</p>
                </div>
              ))}
            </div>
          </section>
        </>
      )}
    </div>
  );
};

export default AccountPage;
