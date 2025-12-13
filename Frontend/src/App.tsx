import { useEffect, useState } from 'react';
import { Routes, Route, Link, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import './styles/App.css';
import Products from './pages/Products';
import LoginPage from './pages/login';
import RegisterPage from './pages/register';
import ProductDetail from './pages/ProductDetail';
import CartPage from './pages/CartPage';
import AccountPage from './pages/AccountPage';
import ProtectedRoute from './components/ProtectedRoute';
import AdminRoute from './components/AdminRoute';
import AdminPage from './pages/AdminPage';
import { useAuth } from './context/AuthContext';

function App() {
  const { t, i18n } = useTranslation();
  const { user, logout, token } = useAuth();
  const [theme, setTheme] = useState(() => localStorage.getItem('theme') || 'light');

  useEffect(() => {
    document.body.className = '';
    document.body.classList.add(theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  const toggleTheme = () => setTheme((prev) => (prev === 'light' ? 'dark' : 'light'));
  const toggleLanguage = () => i18n.changeLanguage(i18n.language === 'tr' ? 'en' : 'tr');

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand">
          <Link to="/products" className="logo">
            {t('site_name')}
          </Link>
        </div>
        <nav>
          <ul className="nav-links">
            <li>
              <Link to="/products">{t('products')}</Link>
            </li>
            <li>
              <Link to="/cart">{t('cart')}</Link>
            </li>
            {token && (
              <li>
                <Link to="/account">{t('account')}</Link>
              </li>
            )}
            {token && user?.roles?.includes('Admin') && (
              <li>
                <Link to="/admin">{t('admin')}</Link>
              </li>
            )}
          </ul>
        </nav>
        <div className="header-actions">
          <button className="theme-toggle-button" onClick={toggleTheme} aria-label={t('theme_toggle')}>
            {theme === 'light' ? '🌙' : '☀️'}
          </button>
          <button className="language-toggle-button" onClick={toggleLanguage}>
            {t('toggle_language')}
          </button>
          {token ? (
            <div className="user-chip">
              <span>
                {user?.firstName} {user?.lastName}
              </span>
              <button onClick={logout} className="link-button">
                {t('logout')}
              </button>
            </div>
          ) : (
            <div className="auth-links">
              <Link to="/login">{t('login')}</Link>
              <Link to="/register">{t('register')}</Link>
            </div>
          )}
        </div>
      </header>

      <main className="main-content">
        <Routes>
          <Route path="/" element={<Navigate to="/products" replace />} />
          <Route path="/products" element={<Products />} />
          <Route path="/products/:id" element={<ProductDetail />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/cart" element={<CartPage />} />
            <Route path="/account" element={<AccountPage />} />
          </Route>
          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<AdminPage />} />
          </Route>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
