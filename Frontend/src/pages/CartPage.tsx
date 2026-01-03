import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'react-toastify';
import { api } from '../lib/api';
import type { CartItem, CartSummary } from '../lib/api';
import { useAuth } from '../context/AuthContext';
import '../styles/Cart.css';

const MIN_ADDRESS_LENGTH = 10;

const CartPage = () => {
  const { token } = useAuth();
  const { t } = useTranslation();
  const [cart, setCart] = useState<CartSummary | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [address, setAddress] = useState<string>('');
  const [addressTouched, setAddressTouched] = useState<boolean>(false);
  const [itemLoading, setItemLoading] = useState<number | null>(null);
  const [checkoutLoading, setCheckoutLoading] = useState<boolean>(false);
  const [purchasingItemId, setPurchasingItemId] = useState<number | null>(null);

  // Adres validasyonu
  const isAddressValid = address.trim().length >= MIN_ADDRESS_LENGTH;
  const showAddressError = addressTouched && !isAddressValid && address.length > 0;

  const loadCart = async () => {
    if (!token) return;
    setLoading(true);
    setError(null);
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
    try {
      await api.updateCartItem(item.cartItemID, count, token);
      toast.success(t('cart_item_updated'));
      await loadCart();
    } catch (err: any) {
      toast.error(err.message || t('cart_update_error'));
    } finally {
      setItemLoading(null);
    }
  };

  const removeItem = async (item: CartItem) => {
    if (!token || itemLoading === item.cartItemID) return;
    setItemLoading(item.cartItemID);
    setError(null);
    try {
      await api.removeCartItem(item.cartItemID, token);
      toast.success(t('cart_item_removed'));
      await loadCart();
    } catch (err: any) {
      toast.error(err.message || t('cart_remove_error'));
    } finally {
      setItemLoading(null);
    }
  };

  const clearCart = async () => {
    if (!token || loading) return;
    setLoading(true);
    setError(null);
    try {
      await api.clearCart(token);
      toast.success(t('cart_cleared'));
      await loadCart();
    } catch (err: any) {
      toast.error(err.message || t('cart_clear_error'));
    } finally {
      setLoading(false);
    }
  };

  const checkout = async () => {
    if (!token || !cart || checkoutLoading) return;

    setAddressTouched(true);

    if (!address || address.trim() === '') {
      toast.warning(t('address_required'));
      return;
    }

    if (!isAddressValid) {
      toast.warning(t('address_min_length'));
      return;
    }

    setCheckoutLoading(true);
    setError(null);
    try {
      const res = await api.createOrder(address, token);
      toast.success(`${t('order_placed_success')} #${res.data?.orderID ?? ''}`);
      setAddress('');
      setAddressTouched(false);
      await loadCart();
    } catch (err: any) {
      toast.error(err.message || t('checkout_error'));
    } finally {
      setCheckoutLoading(false);
    }
  };

  const purchaseSingleItem = async (item: CartItem) => {
    if (!token || purchasingItemId === item.cartItemID) return;

    setAddressTouched(true);

    if (!address || address.trim() === '') {
      toast.warning(t('address_required'));
      return;
    }

    if (!isAddressValid) {
      toast.warning(t('address_min_length'));
      return;
    }

    setPurchasingItemId(item.cartItemID);
    setError(null);
    try {
      const res = await api.purchaseSingleItem(item.cartItemID, address, token);
      toast.success(`${t('item_purchased_success')} #${res.data?.orderID ?? ''}`);
      await loadCart();
    } catch (err: any) {
      toast.error(err.message || t('purchase_error'));
    } finally {
      setPurchasingItemId(null);
    }
  };

  if (loading && !cart) return <p>{t('loading_cart')}</p>;
  if (error && !cart) return <p className="error">{error}</p>;
  if (!cart || cart.items.length === 0) return <p>✨ {t('cart_empty')}</p>;

  return (
    <div className="cart-page">
      <h2>🛒 {t('cart_title')}</h2>
      {error && <p className="error-message">{error}</p>}
      <div className="cart-list">
        {cart.items.map((item) => (
          <div key={item.cartItemID} className="cart-item">
            <img
              src={item.productImageUrl || 'https://via.placeholder.com/60'}
              alt={item.productName}
              className="cart-item-image"
            />
            <div className="cart-item-info">
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
            <div className="cart-item-actions">
              <button
                className="buy-button"
                onClick={() => purchaseSingleItem(item)}
                disabled={purchasingItemId === item.cartItemID || !isAddressValid}
                title={!isAddressValid ? t('address_min_length') : t('buy_this_item')}
              >
                🛍️ {t('buy_this_item')} {purchasingItemId === item.cartItemID && <small>⏳</small>}
              </button>
              <button
                className="link-button"
                onClick={() => removeItem(item)}
                disabled={itemLoading === item.cartItemID}
              >
                🗑️ {t('remove')}
              </button>
            </div>
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
        <div className="address-input-wrapper">
          <textarea
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            onBlur={() => setAddressTouched(true)}
            placeholder={t('address_placeholder')}
            rows={3}
            className={showAddressError ? 'input-error' : ''}
          />
          {showAddressError && (
            <p className="address-hint">⚠️ {t('address_min_length')}</p>
          )}
          {!showAddressError && address.length > 0 && address.length < MIN_ADDRESS_LENGTH && (
            <p className="address-counter">
              {address.length}/{MIN_ADDRESS_LENGTH} {t('characters')}
            </p>
          )}
        </div>
        <div className="summary-actions">
          <button onClick={clearCart} disabled={loading}>
            🧹 {t('clear_cart')}
          </button>
          <button onClick={checkout} disabled={checkoutLoading || !isAddressValid}>
            ✅ {t('checkout_all')} {checkoutLoading && <small>⏳</small>}
          </button>
        </div>
      </div>
    </div>
  );
};

export default CartPage;
