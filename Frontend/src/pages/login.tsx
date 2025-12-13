import React, { useEffect, useState } from 'react';
import { useNavigate, Link, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import '../styles/login.css';
import { useAuth } from '../context/AuthContext';

const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const navigate = useNavigate();
  const { login, token } = useAuth();
  const { t } = useTranslation();

  useEffect(() => {
    if (token) {
      navigate('/products', { replace: true });
    }
  }, [token, navigate]);

  if (token) {
    return <Navigate to="/products" replace />;
  }

  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setSuccessMessage(null);

    try {
      await login(email, password);
      setSuccessMessage(t('login_success'));
      setTimeout(() => navigate('/products'), 1000);
    } catch (err: any) {
      setError(err.message || 'Hata');
    }
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <h2>{t('login_heading')}</h2>
        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label htmlFor="email">{t('email')}</label>
            <input
              type="email"
              id="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="password">{t('password')}</label>
            <div className="password-input-container">
              <input
                type={showPassword ? 'text' : 'password'}
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
              <button
                type="button"
                className="toggle-password-visibility"
                onClick={togglePasswordVisibility}
              >
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
          </div>
          <button type="submit" className="login-button">
            {t('login')}
          </button>
          {error && <p className="error-message">{error}</p>}
          {successMessage && <p className="success-message">{successMessage}</p>}
        </form>
        <div className="register-link">
          <p>
            {t('register')}? <Link to="/register">{t('register')}</Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
