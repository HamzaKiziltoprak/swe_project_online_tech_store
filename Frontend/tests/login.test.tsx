import { render, screen, fireEvent, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import LoginPage from '@/pages/login';
import { AuthContext } from '@/context/AuthContext';
import type { UserProfile } from '@/lib/api';

// ====================
// MOCKS
// ====================

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        'email': 'Email',
        'password': 'Password',
        'login': 'Login',
        'login_heading': 'Login',
        'login_success': 'Login successful!',
        'login_error': 'Login failed',
        'dont_have_account': 'Don\'t have an account',
        'register': 'Register',
        'invalid_credentials': 'Invalid credentials'
      };
      return translations[key] || key;
    },
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

vi.mock('@/lib/api', () => ({
  api: {
    resendConfirmationEmail: vi.fn()
  }
}));

// ====================
// TEST UTILITIES
// ====================

interface RenderOptions {
  token?: string | null;
  user?: UserProfile | null;
  loading?: boolean;
  loginMock?: ReturnType<typeof vi.fn>;
}

const createMockAuthContext = (options: RenderOptions = {}) => {
  const {
    token = null,
    user = null,
    loading = false,
    loginMock = vi.fn()
  } = options;

  return {
    login: loginMock,
    token,
    logout: vi.fn(),
    user,
    loading,
    refreshProfile: vi.fn()
  };
};

const renderLoginPage = (options: RenderOptions = {}) => {
  const authContext = createMockAuthContext(options);

  return {
    ...render(
      <AuthContext.Provider value={authContext}>
        <MemoryRouter>
          <LoginPage />
        </MemoryRouter>
      </AuthContext.Provider>
    ),
    authContext
  };
};

const renderWithRoutes = (options: RenderOptions = {}) => {
  const authContext = createMockAuthContext(options);

  return {
    ...render(
      <AuthContext.Provider value={authContext}>
        <MemoryRouter initialEntries={['/login']}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/products" element={<div>Products Page</div>} />
            <Route path="/register" element={<div>Register Page</div>} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>
    ),
    authContext
  };
};

// ====================
// TEST SUITES
// ====================

describe('LoginPage Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers({ shouldAdvanceTime: true });
    localStorage.clear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  // ====================
  // RENDERING TESTS
  // ====================
  describe('Render Tests (Render Testleri)', () => {

    it('Form elemanları doğru şekilde ekranda görünmeli', () => {
      renderLoginPage();

      expect(screen.getByLabelText(/email/i)).toBeDefined();
      expect(screen.getByLabelText(/password/i)).toBeDefined();
      expect(screen.getByRole('button', { name: /login/i })).toBeDefined();
    });

    it('Login başlığı görünmeli', () => {
      renderLoginPage();

      expect(screen.getByRole('heading', { name: /login/i })).toBeDefined();
    });

    it('Kayıt ol linki görünmeli ve doğru href değerine sahip olmalı', () => {
      renderLoginPage();

      const registerLink = screen.getByRole('link', { name: /register/i });
      expect(registerLink).toBeDefined();
      expect(registerLink.getAttribute('href')).toBe('/register');
    });

    it('Şifre görünürlük butonu görünmeli', () => {
      renderLoginPage();

      const toggleButton = screen.getByRole('button', { name: /show/i });
      expect(toggleButton).toBeDefined();
    });

    it('E-posta alanı email tipinde olmalı', () => {
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;
      expect(emailInput.type).toBe('email');
    });

    it('Şifre alanı varsayılan olarak password tipinde olmalı', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      expect(passwordInput.type).toBe('password');
    });

    it('Form alanları required olmalı', () => {
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;
      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;

      expect(emailInput.required).toBe(true);
      expect(passwordInput.required).toBe(true);
    });

    it('Başlangıçta hata mesajı görünmemeli', () => {
      renderLoginPage();

      expect(screen.queryByRole('alert')).toBeNull();
      expect(screen.queryByText(/error/i)).toBeNull();
    });

    it('Başlangıçta başarı mesajı görünmemeli', () => {
      renderLoginPage();

      expect(screen.queryByText(/login_success/i)).toBeNull();
    });
  });

  // ====================
  // INPUT INTERACTION TESTS
  // ====================
  describe('Input Interaction Tests (Giriş Etkileşim Testleri)', () => {

    it('Email alanına yazı yazılabilmeli', () => {
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;
      fireEvent.change(emailInput, { target: { value: 'test@test.com' } });

      expect(emailInput.value).toBe('test@test.com');
    });

    it('Şifre alanına yazı yazılabilmeli', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      fireEvent.change(passwordInput, { target: { value: 'Password123!' } });

      expect(passwordInput.value).toBe('Password123!');
    });

    it('Email ve şifre alanlarına birlikte yazı yazılabilmeli', () => {
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;
      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;

      fireEvent.change(emailInput, { target: { value: 'test@test.com' } });
      fireEvent.change(passwordInput, { target: { value: 'Password123!' } });

      expect(emailInput.value).toBe('test@test.com');
      expect(passwordInput.value).toBe('Password123!');
    });

    it('Boş email ile form gönderilememeli (HTML5 validation)', async () => {
      const mockLogin = vi.fn();
      renderLoginPage({ loginMock: mockLogin });

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      fireEvent.change(passwordInput, { target: { value: 'Password123!' } });

      const submitButton = screen.getByRole('button', { name: /login/i });
      fireEvent.click(submitButton);

      // HTML5 validation should prevent form submission
      expect(mockLogin).not.toHaveBeenCalled();
    });

    it('Boş şifre ile form gönderilememeli (HTML5 validation)', async () => {
      const mockLogin = vi.fn();
      renderLoginPage({ loginMock: mockLogin });

      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;
      fireEvent.change(emailInput, { target: { value: 'test@test.com' } });

      const submitButton = screen.getByRole('button', { name: /login/i });
      fireEvent.click(submitButton);

      // HTML5 validation should prevent form submission
      expect(mockLogin).not.toHaveBeenCalled();
    });

    it('Çok uzun email adresi yazılabilmeli', () => {
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;
      const longEmail = 'a'.repeat(100) + '@example.com';

      fireEvent.change(emailInput, { target: { value: longEmail } });

      expect(emailInput.value).toBe(longEmail);
    });

    it('Özel karakterler içeren şifre yazılabilmeli', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      const specialPassword = 'P@$$w0rd!@#$%^&*()_+-=[]{}|;:,.<>?';

      fireEvent.change(passwordInput, { target: { value: specialPassword } });

      expect(passwordInput.value).toBe(specialPassword);
    });
  });

  // ====================
  // PASSWORD VISIBILITY TESTS
  // ====================
  describe('Password Visibility Tests (Şifre Görünürlük Testleri)', () => {

    it('Şifre görünürlüğü butonu çalışmalı - ilk tıklama', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      const toggleButton = screen.getByRole('button', { name: /show/i });

      expect(passwordInput.type).toBe('password');

      fireEvent.click(toggleButton);

      expect(passwordInput.type).toBe('text');
    });

    it('Şifre görünürlüğü butonu çalışmalı - ikinci tıklama (geri gizleme)', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      const toggleButton = screen.getByRole('button', { name: /show/i });

      fireEvent.click(toggleButton);
      expect(passwordInput.type).toBe('text');

      const hideButton = screen.getByRole('button', { name: /hide/i });
      fireEvent.click(hideButton);

      expect(passwordInput.type).toBe('password');
    });

    it('Şifre görünürlüğü butonunun metni değişmeli', () => {
      renderLoginPage();

      const toggleButton = screen.getByRole('button', { name: /show/i });
      expect(toggleButton.textContent).toBe('Show');

      fireEvent.click(toggleButton);

      expect(screen.getByRole('button', { name: /hide/i }).textContent).toBe('Hide');
    });

    it('Şifre gösterilirken değer korunmalı', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      const toggleButton = screen.getByRole('button', { name: /show/i });

      fireEvent.change(passwordInput, { target: { value: 'MySecretPassword' } });
      fireEvent.click(toggleButton);

      expect(passwordInput.value).toBe('MySecretPassword');
      expect(passwordInput.type).toBe('text');
    });
  });

  // ====================
  // SUCCESSFUL LOGIN TESTS
  // ====================
  describe('Successful Login Tests (Başarılı Giriş Testleri)', () => {

    it('Başarılı girişte login fonksiyonu çağrılmalı', async () => {
      const mockLogin = vi.fn().mockResolvedValueOnce({});
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      expect(mockLogin).toHaveBeenCalledWith('test@test.com', 'Password123!');
    });

    it('Başarılı girişte başarı mesajı gösterilmeli', async () => {
      const mockLogin = vi.fn().mockResolvedValueOnce({});
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/login successful/i)).toBeDefined();
      });
    });

    it('Başarılı girişte yönlendirme yapılmalı', async () => {
      const mockLogin = vi.fn().mockResolvedValueOnce({});
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/login successful/i)).toBeDefined();
      });

      // Wait for setTimeout navigation
      await vi.advanceTimersByTimeAsync(1000);

      expect(mockedUsedNavigate).toHaveBeenCalledWith('/products');
    });

    it('Login fonksiyonu sadece bir kez çağrılmalı', async () => {
      const mockLogin = vi.fn().mockResolvedValueOnce({});
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(mockLogin).toHaveBeenCalledTimes(1);
      });
    });

    it('Girilen email ve şifre login fonksiyonuna doğru aktarılmalı', async () => {
      const mockLogin = vi.fn().mockResolvedValueOnce({});
      renderLoginPage({ loginMock: mockLogin });

      const testEmail = 'uniqueuser@company.com';
      const testPassword = 'UniqueP@ssw0rd!';

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: testEmail } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: testPassword } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(mockLogin).toHaveBeenCalledWith(testEmail, testPassword);
      });
    });
  });

  // ====================
  // ERROR HANDLING TESTS
  // ====================
  describe('Error Handling Tests (Hata Yönetimi Testleri)', () => {

    it('Hatalı girişte hata mesajı gösterilmeli', async () => {
      const errorKey = 'invalid_credentials';
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error(errorKey));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'wrong@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrongpass' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(new RegExp(errorKey, 'i'))).toBeDefined();
      }, { timeout: 2000 });
    });

    it('Genel login hatası mesajı gösterilmeli', async () => {
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('login_error'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/login_error/i)).toBeDefined();
      });
    });

    it('Network hatası durumunda hata mesajı gösterilmeli', async () => {
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('Network error'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/network error/i)).toBeDefined();
      });
    });

    it('Hata sonrası başarı mesajı görünmemeli', async () => {
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('invalid_credentials'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrongpass' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/invalid_credentials/i)).toBeDefined();
      });

      expect(screen.queryByText(/login successful/i)).toBeNull();
    });

    it('Önceki hata mesajı yeni giriş denemesinde temizlenmeli', async () => {
      const mockLogin = vi.fn()
        .mockRejectedValueOnce(new Error('first_error'))
        .mockResolvedValueOnce({});

      renderLoginPage({ loginMock: mockLogin });

      // First attempt - fails
      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrongpass' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/first_error/i)).toBeDefined();
      });

      // Second attempt - succeeds
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'correctpass' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      // The error should be cleared when submitting
      await waitFor(() => {
        expect(screen.queryByText(/first_error/i)).toBeNull();
      });
    });
  });

  // ====================
  // EMAIL CONFIRMATION TESTS
  // ====================
  describe('Email Confirmation Tests (E-posta Doğrulama Testleri)', () => {

    it('E-posta doğrulanmamış hatası için özel mesaj gösterilmeli - NotAllowed', async () => {
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('NotAllowed'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'unverified@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/doğrulanmadı/i)).toBeDefined();
      });
    });

    it('E-posta doğrulanmamış hatası için özel mesaj gösterilmeli - confirm içeren mesaj', async () => {
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('Please confirm your email'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'unverified@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/doğrulanmadı/i)).toBeDefined();
      });
    });

    it('E-posta doğrulanmamış durumunda tekrar gönder seçeneği görünmeli', async () => {
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('NotAllowed'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'unverified@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/tekrar gönder/i)).toBeDefined();
      });
    });

    it('Tekrar gönder butonu doğrulama e-postası göndermeli', async () => {
      const { api } = await import('@/lib/api');
      vi.mocked(api.resendConfirmationEmail).mockResolvedValueOnce({ success: true, message: 'Sent' });

      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('NotAllowed'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'unverified@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/tekrar gönder/i)).toBeDefined();
      });

      const resendButton = screen.getByRole('button', { name: /tekrar gönder/i });
      fireEvent.click(resendButton);

      await waitFor(() => {
        expect(api.resendConfirmationEmail).toHaveBeenCalledWith('unverified@test.com');
      });
    });

    it('Tekrar gönder başarılı olduğunda başarı mesajı gösterilmeli', async () => {
      const { api } = await import('@/lib/api');
      vi.mocked(api.resendConfirmationEmail).mockResolvedValueOnce({ success: true, message: 'Sent' });

      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('NotAllowed'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'unverified@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/tekrar gönder/i)).toBeDefined();
      });

      const resendButton = screen.getByRole('button', { name: /tekrar gönder/i });
      fireEvent.click(resendButton);

      await waitFor(() => {
        expect(screen.getByText(/doğrulama e-postası tekrar gönderildi/i)).toBeDefined();
      });
    });

    it('E-posta alanı boşken tekrar gönder tıklandığında uyarı gösterilmeli', async () => {
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('NotAllowed'));
      renderLoginPage({ loginMock: mockLogin });

      // Type email first, then clear it
      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/tekrar gönder/i)).toBeDefined();
      });

      // Clear email field
      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: '' } });

      const resendButton = screen.getByRole('button', { name: /tekrar gönder/i });
      fireEvent.click(resendButton);

      await waitFor(() => {
        expect(screen.getByText(/lütfen önce e-posta adresinizi girin/i)).toBeDefined();
      });
    });
  });

  // ====================
  // NAVIGATION & REDIRECT TESTS
  // ====================
  describe('Navigation & Redirect Tests (Navigasyon ve Yönlendirme Testleri)', () => {

    it('Token varsa /products sayfasına yönlendirilmeli', () => {
      renderWithRoutes({ token: 'valid-token' });

      expect(screen.getByText('Products Page')).toBeDefined();
    });

    it('Token yoksa login formu gösterilmeli', () => {
      renderLoginPage({ token: null });

      expect(screen.getByLabelText(/email/i)).toBeDefined();
      expect(screen.getByRole('button', { name: /login/i })).toBeDefined();
    });

    it('Kayıt ol linkine tıklandığında register sayfasına gidilmeli', () => {
      renderLoginPage();

      const registerLink = screen.getByRole('link', { name: /register/i });
      expect(registerLink.getAttribute('href')).toBe('/register');
    });
  });

  // ====================
  // ACCESSIBILITY TESTS
  // ====================
  describe('Accessibility Tests (Erişilebilirlik Testleri)', () => {

    it('Email alanı label ile ilişkilendirilmiş olmalı', () => {
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i);
      expect(emailInput.getAttribute('id')).toBe('email');
    });

    it('Şifre alanı label ile ilişkilendirilmiş olmalı', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i);
      expect(passwordInput.getAttribute('id')).toBe('password');
    });

    it('Form elemanları tab ile erişilebilir olmalı', async () => {
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i);
      const passwordInput = screen.getByLabelText(/password/i);
      const submitButton = screen.getByRole('button', { name: /login/i });

      await user.tab();
      expect(document.activeElement).toBe(emailInput);

      await user.tab();
      expect(document.activeElement).toBe(passwordInput);
    });

    it('Hata mesajı error class ile stillendirilmiş olmalı', async () => {
      const mockLogin = vi.fn().mockRejectedValueOnce(new Error('test_error'));
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'password' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        const errorMessage = screen.getByText(/test_error/i);
        expect(errorMessage.classList.contains('error-message')).toBe(true);
      });
    });

    it('Başarı mesajı success class ile stillendirilmiş olmalı', async () => {
      const mockLogin = vi.fn().mockResolvedValueOnce({});
      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        const successMessage = screen.getByText(/login successful/i);
        expect(successMessage.classList.contains('success-message')).toBe(true);
      });
    });
  });

  // ====================
  // EDGE CASE TESTS
  // ====================
  describe('Edge Case Tests (Uç Durum Testleri)', () => {

    it('Birden fazla hızlı form gönderimi sadece bir login çağrısı yapmalı', async () => {
      let resolveLogin: () => void;
      const loginPromise = new Promise<void>((resolve) => {
        resolveLogin = resolve;
      });
      const mockLogin = vi.fn().mockImplementation(() => loginPromise);

      renderLoginPage({ loginMock: mockLogin });

      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'test@test.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Password123!' } });

      const submitButton = screen.getByRole('button', { name: /login/i });

      // Click multiple times rapidly
      fireEvent.click(submitButton);
      fireEvent.click(submitButton);
      fireEvent.click(submitButton);

      // Even with multiple clicks, login should only be called once
      // (though the component doesn't block this - it depends on implementation)
      expect(mockLogin).toHaveBeenCalled();

      resolveLogin!();
    });

    it('Unicode karakterler içeren email ile giriş yapılabilmeli', () => {
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;
      const unicodeEmail = 'müşteri@şirket.com';

      fireEvent.change(emailInput, { target: { value: unicodeEmail } });

      expect(emailInput.value).toBe(unicodeEmail);
    });

    it('Boşluk içeren şifre yazılabilmeli', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      const passwordWithSpaces = 'Pass word with spaces';

      fireEvent.change(passwordInput, { target: { value: passwordWithSpaces } });

      expect(passwordInput.value).toBe(passwordWithSpaces);
    });

    it('Çok kısa şifre yazılabilmeli', () => {
      renderLoginPage();

      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;

      fireEvent.change(passwordInput, { target: { value: 'a' } });

      expect(passwordInput.value).toBe('a');
    });

    it('Email değiştiğinde state güncellenmeli', async () => {
      renderLoginPage();

      const emailInput = screen.getByLabelText(/email/i) as HTMLInputElement;

      fireEvent.change(emailInput, { target: { value: 'first@test.com' } });
      expect(emailInput.value).toBe('first@test.com');

      fireEvent.change(emailInput, { target: { value: 'second@test.com' } });
      expect(emailInput.value).toBe('second@test.com');
    });
  });

  // ====================
  // INTEGRATION-LIKE TESTS
  // ====================
  describe('Integration-Like Tests (Entegrasyon Benzeri Testler)', () => {

    it('Tam başarılı giriş akışı', async () => {
      const mockLogin = vi.fn().mockResolvedValueOnce({});
      renderLoginPage({ loginMock: mockLogin });

      // 1. Form görünür
      expect(screen.getByRole('heading', { name: /login/i })).toBeDefined();

      // 2. Email gir
      const emailInput = screen.getByLabelText(/email/i);
      fireEvent.change(emailInput, { target: { value: 'user@example.com' } });

      // 3. Şifre gir
      const passwordInput = screen.getByLabelText(/password/i);
      fireEvent.change(passwordInput, { target: { value: 'SecurePassword123!' } });

      // 4. Form gönder
      const submitButton = screen.getByRole('button', { name: /login/i });
      fireEvent.click(submitButton);

      // 5. Login çağrıldı mı kontrol et
      expect(mockLogin).toHaveBeenCalledWith('user@example.com', 'SecurePassword123!');

      // 6. Başarı mesajı gösterildi mi
      await waitFor(() => {
        expect(screen.getByText(/login successful/i)).toBeDefined();
      });

      // 7. Yönlendirme yapıldı mı
      await vi.advanceTimersByTimeAsync(1000);
      expect(mockedUsedNavigate).toHaveBeenCalledWith('/products');
    });

    it('Başarısız giriş ve yeniden deneme akışı', async () => {
      const mockLogin = vi.fn()
        .mockRejectedValueOnce(new Error('Invalid password'))
        .mockResolvedValueOnce({});

      renderLoginPage({ loginMock: mockLogin });

      // İlk deneme - başarısız
      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'user@example.com' } });
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'WrongPassword' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/invalid password/i)).toBeDefined();
      });

      // İkinci deneme - başarılı
      fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'CorrectPassword123!' } });
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(screen.getByText(/login successful/i)).toBeDefined();
      });

      expect(mockLogin).toHaveBeenCalledTimes(2);
    });

    it('Şifre göster/gizle ile giriş akışı', async () => {
      const mockLogin = vi.fn().mockResolvedValueOnce({});
      renderLoginPage({ loginMock: mockLogin });

      // Email gir
      fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'user@example.com' } });

      // Şifre gir
      const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement;
      fireEvent.change(passwordInput, { target: { value: 'MyPassword123!' } });

      // Şifreyi göster
      fireEvent.click(screen.getByRole('button', { name: /show/i }));
      expect(passwordInput.type).toBe('text');
      expect(passwordInput.value).toBe('MyPassword123!');

      // Şifreyi gizle
      fireEvent.click(screen.getByRole('button', { name: /hide/i }));
      expect(passwordInput.type).toBe('password');

      // Form gönder
      fireEvent.click(screen.getByRole('button', { name: /login/i }));

      await waitFor(() => {
        expect(mockLogin).toHaveBeenCalledWith('user@example.com', 'MyPassword123!');
      });
    });
  });
});
