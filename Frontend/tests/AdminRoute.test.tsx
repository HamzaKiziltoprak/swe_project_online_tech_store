import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import AdminRoute from '@/components/AdminRoute';
import { AuthContext } from '@/context/AuthContext';

const renderAdminRoute = (authValue: any) => {
  return render(
    <AuthContext.Provider
      value={{
        ...authValue,
        login: vi.fn(),
        logout: vi.fn(),
        refreshProfile: vi.fn()
      }}
    >
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>

          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<div>Admin Dashboard</div>} />
          </Route>

          <Route path="/login" element={<div>Login Page</div>} />
          <Route path="/products" element={<div>Products Page</div>} />

        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>
  );
};

describe('AdminRoute', () => {

  it('Loading durumunda loading yazısını göstermeli', () => {
    renderAdminRoute({ loading: true });
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('Token yoksa login sayfasına yönlendirmeli', () => {
    renderAdminRoute({ loading: false, token: null });
    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });

  it('Giriş yapmış ama Admin olmayan kullanıcıyı /products sayfasına yönlendirmeli', () => {
    renderAdminRoute({ 
      loading: false, 
      token: 'user-token', 
      user: { roles: ['User'] } 
    });
    expect(screen.getByText('Products Page')).toBeInTheDocument();
  });

  it('Admin rolüne sahip kullanıcıya içeriği göstermeli', () => {
    renderAdminRoute({ 
      loading: false, 
      token: 'admin-token', 
      user: { roles: ['Admin'] } 
    });
    expect(screen.getByText('Admin Dashboard')).toBeInTheDocument();
  });
});
