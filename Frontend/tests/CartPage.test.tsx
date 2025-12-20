import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import CartPage from '@/pages/CartPage'; 
import { AuthContext } from '@/context/AuthContext';
import { api } from '@/lib/api';

const tStable = (key: string) => key;
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: tStable }),
}));

vi.mock('@/lib/api', () => ({
  api: {
    getCart: vi.fn(),
    updateCartItem: vi.fn(),
    removeCartItem: vi.fn(),
    clearCart: vi.fn(),
    createOrder: vi.fn(),
  },
}));

const mockCart = {
  items: [
    {
      cartItemID: 101,
      productName: 'Gaming Mouse',
      price: 500,
      count: 2,
      subtotal: 1000
    },
  ],
  totalItems: 2,
  totalPrice: 1000,
};

describe('CartPage Component', () => {

  beforeEach(() => {
    vi.clearAllMocks();
    (api.getCart as any).mockResolvedValue(mockCart);
  });

  const renderCartPage = (token = 'fake-token') => {
    return render(
      <AuthContext.Provider
        value={{
          token,
          user: null,
          login: vi.fn(),
          logout: vi.fn(),
          loading: false,
          refreshProfile: vi.fn(),
        }}
      >
        <MemoryRouter>
          <CartPage />
        </MemoryRouter>
      </AuthContext.Provider>
    );
  };

  it('Ürün miktarı artırıldığında API çağrılmalı ve sepet yenilenmeli', async () => {
    renderCartPage();

    const plusButton = await screen.findByText('+');

    await act(async () => {
      fireEvent.click(plusButton);
    });

    expect(api.updateCartItem).toHaveBeenCalledWith(101, 3, 'fake-token');
  
    expect(api.getCart).toHaveBeenCalledTimes(2);
  });

  it('Checkout süreci: Adres girilmeli ve sipariş verilmeli', async () => {

    (api.createOrder as any).mockResolvedValueOnce({
      data: { orderID: 12345 }
    });

    renderCartPage();

    const addressInput = await screen.findByPlaceholderText(/address_placeholder/i);
    const checkoutButton = screen.getByText(/checkout/i);

    await act(async () => {
      fireEvent.change(addressInput, { target: { value: 'Test Address' } });
    });

    await act(async () => {
      fireEvent.click(checkoutButton);
    });

    expect(api.createOrder).toHaveBeenCalledWith('Test Address', 'fake-token');
  });

  it('Silme butonuna basıldığında API çağrılmalı', async () => {

    (api.removeCartItem as any).mockResolvedValueOnce({});

    renderCartPage();

    const removeButton = await screen.findByText(/remove/i);

    await act(async () => {
      fireEvent.click(removeButton);
    });
    
    expect(api.removeCartItem).toHaveBeenCalledWith(101, 'fake-token');
  });
});
