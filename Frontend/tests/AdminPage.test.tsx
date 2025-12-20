import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import AdminPage from '@/pages/AdminPage'; 
import { AuthContext } from '@/context/AuthContext';
import { api } from '@/lib/api';

const tStable = (key: string) => key;
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: tStable }),
}));

vi.mock('@/lib/api', () => ({
  api: {
    getAdminStats: vi.fn(),
    getAllUsers: vi.fn(),
    getAllRoles: vi.fn(),
    getAllProducts: vi.fn(),
    getCategories: vi.fn(),
    getBrands: vi.fn(),
    createProduct: vi.fn(),
    updateProduct: vi.fn(),
    deleteProduct: vi.fn(),
    assignRole: vi.fn(),
    removeRole: vi.fn(),
  },
}));

const mockStats = {
  totalProducts: 50,
  activeProducts: 45,
  outOfStockProducts: 5,
  totalCategories: 10,
  totalOrders: 100,
  pendingOrders: 20,
  completedOrders: 70,
  cancelledOrders: 10,
};

const mockProducts = {
  items: [
    {
      productID: 1,
      productName: 'Test Laptop',
      price: 20000,
      stock: 10,
      brand: 'Apple',
      categoryName: 'PC'
    }
  ]
};

describe('AdminPage Component', () => {

  beforeEach(() => {
    vi.clearAllMocks();

    (api.getAdminStats as any).mockResolvedValue(mockStats);
    (api.getAllUsers as any).mockResolvedValue([]);
    (api.getAllRoles as any).mockResolvedValue(['Admin', 'User']);
    (api.getCategories as any).mockResolvedValue([]);
    (api.getBrands as any).mockResolvedValue([]);
    ((api as any).getAllProducts as any).mockResolvedValue(mockProducts);
    ((api as any).deleteProduct as any).mockResolvedValue({});
  });

  const renderAdminPage = () => {
    return render(
      <AuthContext.Provider
        value={{
          token: 'admin-token',
          user: null,
          login: vi.fn(),
          logout: vi.fn(),
          loading: false,
          refreshProfile: vi.fn()
        }}
      >
        <MemoryRouter>
          <AdminPage />
        </MemoryRouter>
      </AuthContext.Provider>
    );
  };

  it('Admin paneli ve istatistikler ekrana gelmeli', async () => {
    renderAdminPage();

    expect(await screen.findByText(/admin_title/i)).toBeInTheDocument();
    expect(screen.getByText('50')).toBeInTheDocument();
  });

  it('Ürün yönetimi sekmesine geçilebilmeli ve ürünler listelenmeli', async () => {
    renderAdminPage();

    const productTab = screen.getByRole('button', { name: /product_management/i });
    
    await act(async () => {
      fireEvent.click(productTab);
    });

    expect(await screen.findByText(/Test Laptop/i)).toBeInTheDocument();
  });

  it('Yeni ürün ekleme formu açılabilmeli', async () => {
    renderAdminPage();
    
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /product_management/i }));
    });

    const addBtn = screen.getByRole('button', { name: /add_new_product/i });
    fireEvent.click(addBtn);

    expect(
      screen.getByText(/product_name/i, { selector: 'label' })
    ).toBeInTheDocument();
  });

  it('Sekme değiştirildiğinde doğru içerik görünmeli', async () => {
    renderAdminPage();

    const userTabButton = screen.getByRole('button', { name: /user_management_title/i });
    const productTabButton = screen.getByRole('button', { name: /product_management/i });

    await act(async () => {
      fireEvent.click(productTabButton);
    });
    expect(
      await screen.findByRole('button', { name: /add_new_product/i })
    ).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(userTabButton);
    });

    expect(
      screen.queryByRole('button', { name: /add_new_product/i })
    ).not.toBeInTheDocument();
  });
});
