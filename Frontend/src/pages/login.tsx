import React, { useEffect, useState } from 'react';
import { useNavigate, Link, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import '../styles/login.css';
import { useAuth } from '../context/AuthContext';
import { api } from '../lib/api';

const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [showResendOption, setShowResendOption] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);
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
    setShowResendOption(false);

    try {
      await login(email, password);
      setSuccessMessage(t('login_success'));
      setTimeout(() => navigate('/products'), 1000);
    } catch (err: any) {
      const errorMessage = err.message || t('login_error');
      // Check if error is about email not being confirmed (Identity API returns "NotAllowed")
      if (errorMessage.toLowerCase().includes('notallowed') || 
          errorMessage.toLowerCase().includes('not allowed') ||
          errorMessage.toLowerCase().includes('confirm') || 
          errorMessage.toLowerCase().includes('verified')) {
        setError('⚠️ E-posta adresiniz henüz doğrulanmadı! Lütfen e-postanızı kontrol edin ve doğrulama bağlantısına tıklayın.');
        setShowResendOption(true);
      } else {
        setError(errorMessage);
      }
    }
  };

  const handleResendConfirmation = async () => {
    if (!email) {
      setError('Lütfen önce e-posta adresinizi girin.');
      return;
    }
    
    setResendLoading(true);
    try {
      await api.resendConfirmationEmail(email);
      setError(null);
      setShowResendOption(false);
      setSuccessMessage('✉️ Doğrulama e-postası tekrar gönderildi! Lütfen gelen kutunuzu kontrol edin.');
    } catch (err: any) {
      setError(err.message || 'E-posta gönderilemedi. Lütfen tekrar deneyin.');
    } finally {
      setResendLoading(false);
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
          {showResendOption && (
            <div className="resend-email-section">
              <p className="resend-hint">Doğrulama e-postasını almadınız mı?</p>
              <button 
                type="button" 
                className="resend-button"
                onClick={handleResendConfirmation}
                disabled={resendLoading}
              >
                {resendLoading ? 'Gönderiliyor...' : '📧 Tekrar Gönder'}
              </button>
            </div>
          )}
          {successMessage && <p className="success-message">{successMessage}</p>}
        </form>
        <div className="register-link">
          <p>
            {t('dont_have_account')}? <Link to="/register">{t('register')}</Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
