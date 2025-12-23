import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import ProtectedRoute from '@/components/ProtectedRoute';
import { AuthContext } from '@/context/AuthContext';

const renderWithAuth = (authValue: any) => {
  return render(
    <AuthContext.Provider value={{ ...authValue, login: vi.fn(), logout: vi.fn(), refreshProfile: vi.fn() }}>
      <MemoryRouter initialEntries={['/profile']}>
        <Routes>
          <Route element={<ProtectedRoute />}>
            <Route path="/profile" element={<div>Profile Content</div>} />
          </Route>
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>
  );
};

describe('ProtectedRoute', () => {
  it('Oturum kontrol edilirken loading mesajı göstermeli', () => {
    renderWithAuth({ loading: true, token: null });
    expect(screen.getByText(/Oturum kontrol ediliyor/i)).toBeInTheDocument();
  });

  it('Token yoksa kullanıcıyı /login sayfasına yönlendirmeli', () => {
    renderWithAuth({ loading: false, token: null });
    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });

  it('Token varsa alt bileşeni (Outlet) render etmeli', () => {
    renderWithAuth({ loading: false, token: 'fake-token' });
    expect(screen.getByText('Profile Content')).toBeInTheDocument();
  });
});