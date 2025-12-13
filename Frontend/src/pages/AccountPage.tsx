import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../lib/api';
import type { FavoriteItem, Order } from '../lib/api';
import { useAuth } from '../context/AuthContext';
import '../styles/Account.css';

const AccountPage = () => {
  const { user, token, refreshProfile } = useAuth();
  const { t } = useTranslation();
  const [favorites, setFavorites] = useState<FavoriteItem[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [loadingFavorites, setLoadingFavorites] = useState<boolean>(true);
  const [loadingOrders, setLoadingOrders] = useState<boolean>(true);

  useEffect(() => {
    if (!token) return;
    refreshProfile();
    api
      .getFavorites(token)
      .then((res) => setFavorites(res.items))
      .finally(() => setLoadingFavorites(false));
    api
      .getOrders(token)
      .then((res) => setOrders(res.items))
      .finally(() => setLoadingOrders(false));
  }, [token]);

  return (
    <div className="account-page">
      <section className="panel">
        <h3>{t('profile_title')}</h3>
        <p>
          {user?.firstName} {user?.lastName}
        </p>
        <p>{user?.email}</p>
        {user?.address && <p>{user.address}</p>}
      </section>

      <section className="panel">
        <h3>{t('favorites_title')}</h3>
        {loadingFavorites && <p>{t('loading')}</p>}
        {!loadingFavorites && !favorites.length && <p>{t('favorites_empty')}</p>}
        <div className="favorites-grid">
          {favorites.map((fav) => (
            <div key={fav.favoriteID} className="favorite-card">
              <p className="name">{fav.productName}</p>
              <p className="price">₺{fav.price}</p>
              <span className="badge">{fav.brand}</span>
            </div>
          ))}
        </div>
      </section>

      <section className="panel">
        <h3>{t('orders_title')}</h3>
        {loadingOrders && <p>{t('loading')}</p>}
        {!loadingOrders && !orders.length && <p>{t('orders_empty')}</p>}
        <div className="orders-list">
          {orders.map((order) => (
            <div key={order.orderID} className="order-card">
              <div className="order-top">
                <strong>#{order.orderID}</strong>
                <span className="badge">{order.status}</span>
              </div>
              <p>{new Date(order.orderDate).toLocaleDateString()}</p>
              <p>
                {t('total_price')}: ₺{order.totalAmount}
              </p>
              <ul>
                {order.items.map((item) => (
                  <li key={item.orderItemID}>
                    {item.productName} x{item.quantity} - ₺{item.unitPrice}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
};

export default AccountPage;
