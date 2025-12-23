import { render, screen, fireEvent, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import Products from '@/pages/Products'; 
import { AuthContext } from '@/context/AuthContext';
import { api } from '@/lib/api';

const tStable = (key: string) => key;
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ 
    t: tStable,
    i18n: { changeLanguage: vi.fn(), language: 'en' }
  }),
}));

const mockedUsedNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockedUsedNavigate };
});

vi.mock('@/lib/api', () => ({
  api: {
    getCategories: vi.fn(),
    getBrands: vi.fn(),
    getProducts: vi.fn(),
    getFavorites: vi.fn(),
    addToCart: vi.fn(),
    toggleFavorite: vi.fn(),
  },
}));

const mockProducts = {
  items: [
    { productID: 1, productName: 'Laptop', price: 15000, imageUrl: 'https://placehold.co/100', brand: 'TechBrand', stock: 5 },
  ],
};

describe('Products Page Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (api.getCategories as any).mockResolvedValue([]);
    (api.getBrands as any).mockResolvedValue([]);
    (api.getProducts as any).mockResolvedValue(mockProducts);
    (api.getFavorites as any).mockResolvedValue({ items: [] });
  });

  const renderProductsPage = (token: string | null = null) => {
    return render(
      <AuthContext.Provider value={{ 
        token, user: null, login: vi.fn(), logout: vi.fn(), loading: false, refreshProfile: vi.fn() 
      }}>
        <MemoryRouter>
          <Products />
        </MemoryRouter>
      </AuthContext.Provider>
    );
  };

  it('Sayfa açıldığında ürünler listelenmeli', async () => {
    renderProductsPage();
    const productTitle = await screen.findByText('Laptop');
    expect(productTitle).toBeInTheDocument();
  });

  it('Filtreler temizle butonu çalışmalı', async () => {
    renderProductsPage();

    await screen.findByText('Laptop');

    const searchInput = screen.getByPlaceholderText('search_placeholder');
    
    await act(async () => {
      fireEvent.change(searchInput, { target: { value: 'Gaming' } });
    });
    expect(searchInput).toHaveValue('Gaming');

    const clearButton = screen.getByText(/clear_filters/i);
    await act(async () => {
      fireEvent.click(clearButton);
    });

    expect(searchInput).toHaveValue('');
  });

  it('Görünüm modu (Grid/List) değiştirilebilmeli', async () => {
    const { container } = renderProductsPage();
    
    await screen.findByText('Laptop');
    
    const listModeButton = screen.getByText(/list/i);
    await act(async () => {
      fireEvent.click(listModeButton);
    });

    await waitFor(() => {
      const listView = container.querySelector('.products-list-view');
      expect(listView).toBeInTheDocument();
    });
  });
});