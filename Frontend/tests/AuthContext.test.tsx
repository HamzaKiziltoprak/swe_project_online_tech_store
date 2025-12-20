import { renderHook, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AuthProvider, useAuth } from '@/context/AuthContext';
import { api } from '@/lib/api';

vi.mock('@/lib/api', () => ({
  api: {
    getProfile: vi.fn(),
    login: vi.fn(),
  },
}));

describe('AuthContext Logic', () => {

  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  const renderAuthHook = () =>
    renderHook(() => useAuth(), {
      wrapper: ({ children }) => <AuthProvider>{children}</AuthProvider>,
    });

  it('Başlangıçta token yoksa loading false olmalı ve user null kalmalı', async () => {
    const { result } = renderAuthHook();

    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.user).toBeNull();
  });

  it('LocalStorage içinde token varsa başlangıçta profil çekilmeli', async () => {
    const mockUser = { id: 1, firstName: 'New', email: 'test@test.com' };

    localStorage.setItem('accessToken', 'valid-token');

    (api.getProfile as any).mockResolvedValue(mockUser);

    const { result } = renderAuthHook();

    await waitFor(() => {
      expect(result.current.user).toEqual(mockUser);
      expect(result.current.token).toBe('valid-token');
    });
  });

  it('login fonksiyonu token kaydetmeli ve user set etmeli', async () => {
    const mockLoginRes = { accessToken: 'new-token' };
    const mockUser = { id: 1, firstName: 'New' };

    (api.login as any).mockResolvedValue(mockLoginRes);
    (api.getProfile as any).mockResolvedValue(mockUser);

    const { result } = renderAuthHook();

    await act(async () => {
      await result.current.login('test@test.com', 'Password123!');
    });

    await waitFor(() => {
      expect(localStorage.getItem('accessToken')).toBe('new-token');
      expect(result.current.user).toEqual(mockUser);
    });
  });

  it('logout fonksiyonu her şeyi temizlemeli', async () => {

    const mockUser = { id: 1, firstName: 'New' };
    localStorage.setItem('accessToken', 'active-token');
    (api.getProfile as any).mockResolvedValue(mockUser);

    const { result } = renderAuthHook();

    await waitFor(() => expect(result.current.user).not.toBeNull());

    await act(async () => {
      result.current.logout();
    });

    await waitFor(() => {
      expect(localStorage.getItem('accessToken')).toBeNull();
      expect(result.current.token).toBeNull();
      expect(result.current.user).toBeNull();
    });
  });

  it('Profil çekme hatası durumunda sistem logout yapmalı', async () => {
    localStorage.setItem('accessToken', 'invalid-token');

    (api.getProfile as any).mockRejectedValue(new Error('Unauthorized'));

    const { result } = renderAuthHook();

    await waitFor(() => {
      expect(result.current.token).toBeNull();
      expect(localStorage.getItem('accessToken')).toBeNull();
    });
  });
});
