import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import LoginPage from '@/pages/login';
import { AuthContext } from '@/context/AuthContext';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { changeLanguage: vi.fn(), language: 'en' }
  }),
  initReactI18next: { type: '3rdParty', init: vi.fn() }
}));

const mockedUsedNavigate = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockedUsedNavigate,
  };
});

describe('LoginPage Component', () => {

  const mockLogin = vi.fn();

  const renderLoginPage = (token = null) => {
    return render(
      <AuthContext.Provider value={{ 
        login: mockLogin, 
        token, 
        logout: vi.fn(), 
        user: null, 
        loading: false, 
        refreshProfile: vi.fn() 
      }}>
        <MemoryRouter>
          <LoginPage />
        </MemoryRouter>
      </AuthContext.Provider>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('Form elemanları doğru şekilde ekranda görünmeli', () => {
    renderLoginPage();

    expect(screen.getByLabelText(/email/i)).toBeDefined();
    expect(screen.getByLabelText(/password/i)).toBeDefined();
    expect(screen.getByRole('button', { name: /login/i })).toBeDefined();
  });

  it('Email ve şifre alanlarına yazı yazılabilmeli', () => {
    renderLoginPage();

    const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;
    const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;

    fireEvent.change(emailInput, { target: { value: 'test@test.com' } });
    fireEvent.change(passwordInput, { target: { value: 'Password123!' } });

    expect(emailInput.value).toBe('test@test.com');
    expect(passwordInput.value).toBe('Password123!');
  });

  it('Şifre görünürlüğü butonu çalışmalı', () => {
    renderLoginPage();

    const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
    const toggleButton = screen.getByRole('button', { name: /show/i });

    expect(passwordInput.type).toBe('password');

    fireEvent.click(toggleButton);
    expect(passwordInput.type).toBe('text');
  });

  it('Başarılı girişte login fonksiyonu çağrılmalı ve yönlendirme yapılmalı', async () => {

    mockLogin.mockResolvedValueOnce({});

    renderLoginPage();

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
    fireEvent.click(screen.getByRole('button', { name: /login/i }));

    expect(mockLogin).toHaveBeenCalledWith('test@test.com', 'Password123!');

    await waitFor(() => {
      expect(screen.getByText(/login_success/i)).toBeDefined();
    });
  });

  it('Hatalı girişte hata mesajı gösterilmeli', async () => {

    const errorKey = 'invalid_credentials';
    mockLogin.mockRejectedValueOnce(new Error(errorKey));
    
    renderLoginPage();

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'wrong@test.com' } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrongpass' } });
    
    fireEvent.click(screen.getByRole('button', { name: /login/i }));

    await waitFor(() => {
      expect(screen.getByText(new RegExp(errorKey, 'i'))).toBeDefined();
    }, { timeout: 2000 });
  });
});
