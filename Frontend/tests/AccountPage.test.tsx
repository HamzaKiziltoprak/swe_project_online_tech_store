import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import AccountPage from '@/pages/AccountPage'; 
import { AuthContext } from '@/context/AuthContext';
import { api } from '@/lib/api';

const tStable = (key: string) => key;
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: tStable }),
}));

vi.mock('@/lib/api', () => ({
  api: {
    getFavorites: vi.fn(),
    getOrders: vi.fn(),
    updateProfile: vi.fn(),
    changePassword: vi.fn(),
  },
}));

const mockUser = {
  firstName: 'New',
  lastName: 'Test',
  email: 'test@test.com',
  address: 'İstanbul, Türkiye',
  roles: ['User']
};

const mockFavorites = {
  items: [
    { favoriteID: 1, productName: 'Favorite Product', price: 100, brand: 'Brand A' }
  ]
};

const mockOrders = {
  items: [
    { orderID: 55, status: 'Completed', orderDate: '2023-10-10', totalAmount: 500, items: [] }
  ]
};

describe('AccountPage Component', () => {

  beforeEach(() => {
    vi.clearAllMocks();

    (api.getFavorites as any).mockResolvedValue(mockFavorites);
    (api.getOrders as any).mockResolvedValue(mockOrders);
  });

  const renderAccountPage = async (user = mockUser) => {
    await act(async () => {
      render(
        <AuthContext.Provider
          value={{ 
            token: 'fake-token',
            user,
            login: vi.fn(),
            logout: vi.fn(),
            loading: false,
            refreshProfile: vi.fn().mockResolvedValue({}) 
          }}
        >
          <MemoryRouter>
            <AccountPage />
          </MemoryRouter>
        </AuthContext.Provider>
      );
    });
  };

  it('Kullanıcı profil bilgilerini doğru şekilde görüntülemeli', async () => {
    await renderAccountPage();

    expect(screen.getByText(/^New$/)).toBeInTheDocument();
    expect(screen.getByText('test@test.com')).toBeInTheDocument();
  });

  it('Profil düzenleme modu açılabilmeli ve form doldurulabilmeli', async () => {
    await renderAccountPage();

    const profilePanel = screen.getByText(/profile_title/i).closest('section');
    const editBtn = profilePanel?.querySelector('.edit-button');
    
    if (editBtn) fireEvent.click(editBtn);

    const firstNameInput = screen.getByLabelText(/first_name/i);
    await act(async () => {
      fireEvent.change(firstNameInput, { target: { value: 'Testing' } });
    });
    
    expect(firstNameInput).toHaveValue('Testing');
  });

  it('Şifre değiştirme validasyonu: Şifreler eşleşmezse hata vermeli', async () => {
    await renderAccountPage();

    const passwordPanel = screen.getByText(/change_password_title/i).closest('section');
    const editBtn = passwordPanel?.querySelector('.edit-button');
    
    if (editBtn) fireEvent.click(editBtn);

    const newPassInput = screen.getByLabelText(/new_password/i);
    const confirmPassInput = screen.getByLabelText(/confirm_password/i);
    
    await act(async () => {
      fireEvent.change(newPassInput, { target: { value: 'Password123!' } });
      fireEvent.change(confirmPassInput, { target: { value: 'Password456!' } });
    });

    const form = screen.getByLabelText(/new_password/i).closest('form');
    await act(async () => {
      if (form) fireEvent.submit(form);
    });

    const errorMsg = await screen.findByText(/passwords_do_not_match/i);
    expect(errorMsg).toBeInTheDocument();
  });

  it('Normal kullanıcı için favoriler ve siparişler listelenmeli', async () => {
    await renderAccountPage();

    expect(await screen.findByText(/Favorite Product/i)).toBeInTheDocument();
    expect(await screen.findByText(/#55/i)).toBeInTheDocument();
  });
});
