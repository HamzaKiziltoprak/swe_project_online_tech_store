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
import RoleRoute from './components/RoleRoute';
import AdminPage from './pages/AdminPage';
import ProductManagerPage from './pages/ProductManagerPage';
import CompanyOwnerPage from './pages/CompanyOwnerPage';
import ConfirmEmail from './pages/ConfirmEmail';
import NotFound from './pages/NotFound';
import { useAuth } from './context/AuthContext';
import 'react-toastify/dist/ReactToastify.css';
import { ToastContainer } from 'react-toastify';
import { Toaster } from 'react-hot-toast';

function App() {
  const { t, i18n } = useTranslation();
  const { user, logout, token } = useAuth();
  const [theme, setTheme] = useState(() => localStorage.getItem('theme') || 'light');
  const isAdmin = user?.roles?.includes('Admin');
  const isProductManager = user?.roles?.includes('ProductManager');
  const isCompanyOwner = user?.roles?.includes('CompanyOwner');

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
          <Link to={isAdmin ? '/admin' : isCompanyOwner ? '/company-owner' : isProductManager ? '/product-manager' : '/products'} className="logo">
            {t('site_name')}
          </Link>
        </div>

        {(token || !isAdmin) && (
          <nav>
            <ul className="nav-links">
              {!isAdmin && !isProductManager && !isCompanyOwner && (
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
              {token && isAdmin && (
                <li>
                  <Link to="/admin">⚙️ {t('admin')}</Link>
                </li>
              )}
              {token && isProductManager && !isAdmin && (
                <li>
                  <Link to="/product-manager">📦 {t('product_manager_title') || 'Product Manager'}</Link>
                </li>
              )}
              {token && isCompanyOwner && !isAdmin && (
                <li>
                  <Link to="/company-owner">📊 {t('company_owner_dashboard')}</Link>
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
                : isCompanyOwner
                  ? `${t('welcome')}, ${user?.firstName}! 📊`
                  : isProductManager
                    ? `${t('welcome')}, ${user?.firstName}! 📦`
                    : `${t('welcome')}, ${user?.firstName}! 🛍️`}
            </h1>
            <p>
              {isAdmin
                ? t('welcome_admin_message')
                : isCompanyOwner
                  ? t('welcome_company_owner_message')
                  : isProductManager
                    ? t('welcome_product_manager_message')
                    : t('welcome_customer_message')}
            </p>
          </div>
        )}

        <Routes>
          <Route path="/" element={
            <Navigate to={
              isAdmin ? '/admin' :
                isCompanyOwner ? '/company-owner' :
                  isProductManager ? '/product-manager' :
                    '/products'
            } replace />
          } />
          <Route
            path="/products"
            element={isAdmin ? <Navigate to="/admin" replace /> : isProductManager ? <Navigate to="/product-manager" replace /> : isCompanyOwner ? <Navigate to="/company-owner" replace /> : <Products />}
          />
          <Route path="/products/:id" element={<ProductDetail />} />
          <Route element={<ProtectedRoute />}>
            <Route
              path="/cart"
              element={isAdmin ? <Navigate to="/admin" replace /> : isProductManager ? <Navigate to="/product-manager" replace /> : isCompanyOwner ? <Navigate to="/company-owner" replace /> : <CartPage />}
            />
            <Route path="/account" element={<AccountPage />} />
          </Route>
          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<AdminPage />} />
          </Route>
          <Route element={<RoleRoute allowedRoles={['ProductManager', 'Admin']} />}>
            <Route path="/product-manager" element={<ProductManagerPage />} />
          </Route>
          <Route element={<RoleRoute allowedRoles={['CompanyOwner', 'Admin']} />}>
            <Route path="/company-owner" element={<CompanyOwnerPage />} />
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
      <Toaster />
    </div>
  );
}

export default App;
