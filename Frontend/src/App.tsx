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
import ConfirmEmail from './pages/ConfirmEmail';
import NotFound from './pages/NotFound';
import { useAuth } from './context/AuthContext';
import 'react-toastify/dist/ReactToastify.css';
import { ToastContainer } from 'react-toastify';

function App() {
  const { t, i18n } = useTranslation();
  const { user, logout, token } = useAuth();
  const [theme, setTheme] = useState(() => localStorage.getItem('theme') || 'light');
  const isAdmin = user?.roles?.includes('Admin');

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
          <Link to={isAdmin ? '/admin' : '/products'} className="logo">
            {t('site_name')}
          </Link>
        </div>

        {(token || !isAdmin) && (
          <nav>
            <ul className="nav-links">
              {!isAdmin && (
                <>
                  <li>
                    <Link to="/products">🛍️ {t('products')}</Link>
                  </li>
                  <li>
                    <Link to="/cart">🛒 {t('cart')}</Link>
                  </li>
                </>
              )}
              {token && (
                <li>
                  <Link to="/account">👤 {t('account')}</Link>
                </li>
              )}
              {isAdmin && (
                <li>
                  <Link to="/admin">⚙️ {t('admin')}</Link>
                </li>
              )}
            </ul>
          </nav>
        )}

        <div className="header-actions">
          <button className="theme-toggle-button" onClick={toggleTheme} title={t('theme_toggle')}>
            {theme === 'light' ? '🌙' : '☀️'}
          </button>
          <button className="language-toggle-button" onClick={toggleLanguage}>
            {t('toggle_language')}
          </button>
          {token ? (
            <div className="user-chip" title={`${user?.firstName} ${user?.lastName}`}>
              <span className="user-avatar">👤</span>
              <button onClick={logout} className="link-button">
                🚪 {t('logout')}
              </button>
            </div>
          ) : (
            <div className="auth-links">
              <Link to="/login">🔐 {t('login')}</Link>
              <Link to="/register">📝 {t('register')}</Link>
            </div>
          )}
        </div>
      </header>

      <main className="main-content">
        {token && (
          <div className="welcome-banner">
            <h1>
              {isAdmin
                ? `${t('welcome')}, ${user?.firstName}! 👋`
                : `${t('welcome')}, ${user?.firstName}! 🛍️`}
            </h1>
            <p>
              {isAdmin
                ? t('welcome_admin_message')
                : t('welcome_customer_message')}
            </p>
          </div>
        )}

        <Routes>
          <Route path="/" element={<Navigate to={isAdmin ? '/admin' : '/products'} replace />} />
          <Route
            path="/products"
            element={isAdmin ? <Navigate to="/admin" replace /> : <Products />}
          />
          <Route path="/products/:id" element={<ProductDetail />} />
          <Route element={<ProtectedRoute />}>
            <Route
              path="/cart"
              element={isAdmin ? <Navigate to="/admin" replace /> : <CartPage />}
            />
            <Route path="/account" element={<AccountPage />} />
          </Route>
          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<AdminPage />} />
          </Route>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/confirm-email" element={<ConfirmEmail />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
      <ToastContainer
        position="top-right"
        autoClose={3000}
        hideProgressBar={false}
        newestOnTop={false}
        closeOnClick
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
        theme={theme === 'dark' ? 'dark' : 'light'}
      />
    </div>
  );
}

export default App;
