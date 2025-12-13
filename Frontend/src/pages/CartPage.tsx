import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../lib/api';
import type { CartItem, CartSummary } from '../lib/api';
import { useAuth } from '../context/AuthContext';
import '../styles/Cart.css';

const CartPage = () => {
  const { token } = useAuth();
  const { t } = useTranslation();
  const [cart, setCart] = useState<CartSummary | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [address, setAddress] = useState<string>('');
  const [orderMessage, setOrderMessage] = useState<string | null>(null);

  const loadCart = () => {
    if (!token) return;
    setLoading(true);
    api
      .getCart(token)
      .then(setCart)
      .catch((err) => setError(err.message || t('cart_title')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadCart();
  }, [token]);

  const updateCount = async (item: CartItem, count: number) => {
    if (!token) return;
    try {
      await api.updateCartItem(item.cartItemID, count, token);
      loadCart();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const removeItem = async (item: CartItem) => {
    if (!token) return;
    try {
      await api.removeCartItem(item.cartItemID, token);
      loadCart();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const clearCart = async () => {
    if (!token) return;
    await api.clearCart(token);
    loadCart();
  };

  const checkout = async () => {
    if (!token || !cart) return;
    setOrderMessage(null);
    try {
      const res = await api.createOrder(address || t('address_placeholder'), token);
      setOrderMessage(`#${res.data?.orderID ?? ''}`);
      loadCart();
    } catch (err: any) {
      alert(err.message || 'Sipariş oluşturulamadı');
    }
  };

  if (loading) return <p>{t('loading')}</p>;
  if (error) return <p className="error">{error}</p>;
  if (!cart || cart.items.length === 0) return <p>{t('cart_empty')}</p>;

  return (
    <div className="cart-page">
      <h2>{t('cart_title')}</h2>
      <div className="cart-list">
        {cart.items.map((item) => (
          <div key={item.cartItemID} className="cart-item">
            <div>
              <p className="name">{item.productName}</p>
              <p className="price">₺{item.price}</p>
            </div>
            <div className="qty">
              <button onClick={() => updateCount(item, Math.max(1, item.count - 1))}>-</button>
              <span>{item.count}</span>
              <button onClick={() => updateCount(item, item.count + 1)}>+</button>
            </div>
            <p className="subtotal">₺{item.subtotal}</p>
            <button className="link-button" onClick={() => removeItem(item)}>
              {t('remove')}
            </button>
          </div>
        ))}
      </div>
      <div className="cart-summary">
        <p>
          {t('total_items')}: {cart.totalItems}
        </p>
        <p>
          {t('total_price')}: ₺{cart.totalPrice}
        </p>
        <textarea
          value={address}
          onChange={(e) => setAddress(e.target.value)}
          placeholder={t('address_placeholder')}
        />
        <div className="summary-actions">
          <button onClick={clearCart}>{t('clear_cart')}</button>
          <button onClick={checkout}>{t('checkout')}</button>
        </div>
        {orderMessage && <p className="success">{orderMessage}</p>}
      </div>
    </div>
  );
};

export default CartPage;
