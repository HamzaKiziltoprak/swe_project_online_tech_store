import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import ProductDetail from '@/pages/ProductDetail'; 
import { AuthContext } from '@/context/AuthContext';
import { api } from '@/lib/api';

// 1. i18n Mock
const tStable = (key: string) => key;
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: tStable }),
}));

// 2. Router Mock
const mockedUsedNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockedUsedNavigate,
  };
});

// 3. API Mock
vi.mock('@/lib/api', () => ({
  api: {
    getProductDetail: vi.fn(),
    getProductSpecifications: vi.fn(),
    getRelatedProducts: vi.fn(),
    getReviews: vi.fn(),
    isFavorite: vi.fn(),
    addToCart: vi.fn(),
    toggleFavorite: vi.fn(),
    addReview: vi.fn(),
  },
}));

const mockProduct = {
  productID: 123,
  productName: 'Super Gaming Laptop',
  brand: 'Monster',
  price: 45000,
  description: 'Harika bir laptop',
  imageUrl: 'https://placehold.co/200'
};

describe('ProductDetail Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (api.getProductDetail as any).mockResolvedValue(mockProduct);
    (api.getProductSpecifications as any).mockResolvedValue([{ specID: 1, specName: 'RAM', specValue: '32GB' }]);
    (api.getRelatedProducts as any).mockResolvedValue([]);
    (api.getReviews as any).mockResolvedValue([]);
    (api.isFavorite as any).mockResolvedValue(false);
  });

  const renderProductDetail = async (token: string | null = 'fake-token') => {
    await act(async () => {
      render(
        <AuthContext.Provider value={{ 
          token, user: null, login: vi.fn(), logout: vi.fn(), loading: false, refreshProfile: vi.fn() 
        }}>
          <MemoryRouter initialEntries={['/products/123']}>
            <Routes>
              <Route path="/products/:id" element={<ProductDetail />} />
            </Routes>
          </MemoryRouter>
        </AuthContext.Provider>
      );
    });
  };

  it('Ürün detayları ve teknik özellikler doğru yüklenmeli', async () => {
    await renderProductDetail();
    expect(await screen.findByText(/Super Gaming Laptop/i)).toBeInTheDocument();
    expect(screen.getByText(/32GB/i)).toBeInTheDocument();
  });

  it('Giriş yapmamış kullanıcı sepete ekleye basınca login sayfasına gitmeli', async () => {
    await renderProductDetail(null);
    const addToCartBtn = await screen.findByRole('button', { name: /add_to_cart/i });
    fireEvent.click(addToCartBtn);
    expect(mockedUsedNavigate).toHaveBeenCalledWith('/login');
  });

  it('Yorum formu doldurulup gönderilebilmeli', async () => {
    (api.addReview as any).mockResolvedValue({});
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {});
    
    await renderProductDetail();

    const textarea = screen.getByPlaceholderText(/comment_label/i);
    const submitBtn = screen.getByRole('button', { name: /submit/i });

    await act(async () => {
      fireEvent.change(textarea, { target: { value: 'Mükemmel ürün!' } });
      fireEvent.click(submitBtn);
    });

    expect(api.addReview).toHaveBeenCalledWith(123, 5, 'Mükemmel ürün!', 'fake-token');
    expect(alertSpy).toHaveBeenCalled();
    alertSpy.mockRestore();
  });

  it('Hata durumunda error mesajı gösterilmeli', async () => {
    (api.getProductDetail as any).mockRejectedValue(new Error('API Error'));
    await renderProductDetail();
    expect(await screen.findByText(/API Error/i)).toBeInTheDocument();
  });
});