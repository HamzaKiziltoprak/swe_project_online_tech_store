import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { api } from '../lib/api';
import type { UserProfile } from '../lib/api';

interface AuthContextType {
  user: UserProfile | null;
  token: string | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('accessToken'));
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    if (!token) {
      setLoading(false);
      return;
    }
    api
      .getProfile(token)
      .then(setUser)
      .catch(() => {
        setUser(null);
        setToken(null);
        localStorage.removeItem('accessToken');
      })
      .finally(() => setLoading(false));
  }, [token]);

  const login = async (email: string, password: string) => {
    const res = await api.login(email, password);
    const newToken = res.accessToken;
    localStorage.setItem('accessToken', newToken);
    setToken(newToken);
    const profile = await api.getProfile(newToken);
    setUser(profile);
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    setUser(null);
    setToken(null);
  };

  const refreshProfile = async () => {
    if (!token) return;
    const profile = await api.getProfile(token);
    setUser(profile);
  };

  const value = useMemo(
    () => ({
      user,
      token,
      loading,
      login,
      logout,
      refreshProfile,
    }),
    [user, token, loading],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};
