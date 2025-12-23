import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import RegisterPage from '@/pages/register'; 
import { api } from '@/lib/api';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

const mockedUsedNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockedUsedNavigate };
});

vi.mock('@/lib/api', () => ({
  api: {
    register: vi.fn(),
  },
}));

describe('RegisterPage Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useRealTimers();
  });

  const renderRegisterPage = () => {
    return render(
      <MemoryRouter>
        <RegisterPage/>
      </MemoryRouter>
    );
  };

  const fillFullForm = (password = 'password123') => {
    fireEvent.change(screen.getByLabelText(/first_name/i), { target: { value: 'Tuna' } });
    fireEvent.change(screen.getByLabelText(/last_name/i), { target: { value: 'Test' } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: 'tuna@test.com' } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: password } });
  };

  it('Tüm form alanları ekranda görünmeli', () => {
    renderRegisterPage();
    expect(screen.getByLabelText(/first_name/i)).toBeInTheDocument();
  });

  it('Şifre 8 karakterden az ise hata vermeli', async () => {
    renderRegisterPage();
    fillFullForm('123'); 
    fireEvent.click(screen.getByRole('button', { name: /register/i }));

    expect(screen.getByText(/password_min_length/i)).toBeInTheDocument();
  });

  it('Başarılı kayıt sonrası başarı mesajı göstermeli ve yönlendirmeli', async () => {

    vi.useFakeTimers();
    (api.register as any).mockResolvedValueOnce({}); 
    
    renderRegisterPage();
    fillFullForm();

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /register/i }));
    });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(0);
    });

    expect(screen.getByText(/register_success/i)).toBeInTheDocument();

    await act(async () => {
      vi.advanceTimersByTime(1200);
    });

    expect(mockedUsedNavigate).toHaveBeenCalledWith('/login');
    vi.useRealTimers();
  });

  it('API hata döndürürse hata mesajını göstermeli', async () => {
    const apiError = 'Email already exists';
    (api.register as any).mockRejectedValueOnce({ message: apiError });
    
    renderRegisterPage();
    fillFullForm();
    
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /register/i }));
    });

    const errorElement = await screen.findByText(new RegExp(apiError, 'i'));
    expect(errorElement).toBeInTheDocument();
  });
});