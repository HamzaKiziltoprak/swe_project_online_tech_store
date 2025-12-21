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
  const [message, setMessage] = useState<string | null>(null);
  const [itemLoading, setItemLoading] = useState<number | null>(null); // To track loading for individual items
  const [checkoutLoading, setCheckoutLoading] = useState<boolean>(false);

  const loadCart = async () => {
    if (!token) return;
    setLoading(true);
    setError(null);
    setMessage(null);
    try {
      const fetchedCart = await api.getCart(token);
      setCart(fetchedCart);
    } catch (err: any) {
      setError(err.message || t('cart_fetch_error'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCart();
  }, [token]);

  const updateCount = async (item: CartItem, count: number) => {
    if (!token || itemLoading === item.cartItemID) return;
    setItemLoading(item.cartItemID);
    setError(null);
    setMessage(null);
    try {
      await api.updateCartItem(item.cartItemID, count, token);
      setMessage(t('cart_item_updated'));
      await loadCart();
    } catch (err: any) {
      setError(err.message || t('cart_update_error'));
    } finally {
      setItemLoading(null);
    }
  };

  const removeItem = async (item: CartItem) => {
    if (!token || itemLoading === item.cartItemID) return;
    setItemLoading(item.cartItemID);
    setError(null);
    setMessage(null);
    try {
      await api.removeCartItem(item.cartItemID, token);
      setMessage(t('cart_item_removed'));
      await loadCart();
    } catch (err: any) {
      setError(err.message || t('cart_remove_error'));
    } finally {
      setItemLoading(null);
    }
  };

  const clearCart = async () => {
    if (!token || loading) return;
    setLoading(true);
    setError(null);
    setMessage(null);
    try {
      await api.clearCart(token);
      setMessage(t('cart_cleared'));
      await loadCart();
    } catch (err: any) {
      setError(err.message || t('cart_clear_error'));
    } finally {
      setLoading(false);
    }
  };

  const checkout = async () => {
    if (!token || !cart || checkoutLoading) return;
    setCheckoutLoading(true);
    setError(null);
    setMessage(null);
    try {
      const res = await api.createOrder(address || t('address_default'), token);
      setMessage(`${t('order_placed_success')} #${res.data?.orderID ?? ''}`);
      setAddress('');
      await loadCart(); // Refresh cart after checkout
    } catch (err: any) {
      setError(err.message || t('checkout_error'));
    } finally {
      setCheckoutLoading(false);
    }
  };

  if (loading && !cart) return <p>{t('loading_cart')}</p>;
  if (error && !cart) return <p className="error">{error}</p>;
  if (!cart || cart.items.length === 0) return <p>✨ {t('cart_empty')}</p>;

  return (
    <div className="cart-page">
      <h2>🛒 {t('cart_title')}</h2>
      {message && <p className="success-message">{message}</p>}
      {error && <p className="error-message">{error}</p>}
      <div className="cart-list">
        {cart.items.map((item) => (
          <div key={item.cartItemID} className="cart-item">
            <div>
              <p className="name">🏷️ {item.productName}</p>
              <p className="price">💰 ₺{item.price}</p>
            </div>
            <div className="qty">
              <button
                onClick={() => updateCount(item, Math.max(1, item.count - 1))}
                disabled={itemLoading === item.cartItemID}
              >
                −
              </button>
              <span>
                {item.count} {itemLoading === item.cartItemID && <small>⏳</small>}
              </span>
              <button
                onClick={() => updateCount(item, item.count + 1)}
                disabled={itemLoading === item.cartItemID}
              >
                +
              </button>
            </div>
            <p className="subtotal">💵 ₺{item.subtotal}</p>
            <button
              className="link-button"
              onClick={() => removeItem(item)}
              disabled={itemLoading === item.cartItemID}
            >
              🗑️ {t('remove')}
            </button>
          </div>
        ))}
      </div>
      <div className="cart-summary">
        <p>
          📊 {t('total_items')}: {cart.totalItems}
        </p>
        <p>
          💳 {t('total_price')}: ₺{cart.totalPrice}
        </p>
        <textarea
          value={address}
          onChange={(e) => setAddress(e.target.value)}
          placeholder={t('address_placeholder')}
          rows={3}
        />
        <div className="summary-actions">
          <button onClick={clearCart} disabled={loading}>
            🧹 {t('clear_cart')}
          </button>
          <button onClick={checkout} disabled={checkoutLoading || !address}>
            ✅ {t('checkout')} {checkoutLoading && <small>⏳</small>}
          </button>
        </div>
      </div>
    </div>
  );
};

export default CartPage;
