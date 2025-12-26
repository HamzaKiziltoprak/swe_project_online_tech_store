import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import '../styles/register.css';
import { api } from '../lib/api';

const RegisterPage: React.FC = () => {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [showPasswordHint, setShowPasswordHint] = useState(false);
  const navigate = useNavigate();
  const { t } = useTranslation();

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    if (password.length < 8) {
      setError(t('password_min_length'));
      return;
    }

    try {
      await api.register({ firstName, lastName, email, password });
      setSuccess(t('register_success'));
      setTimeout(() => navigate('/login'), 1200);
    } catch (err: any) {
      setError(err.message || 'Hata');
    }
  };

  return (
    <div className="register-page">
      <div className="register-container">
        <h2>📝 {t('register_heading')}</h2>
        <form onSubmit={handleSubmit} className="register-form">
          <div className="form-group">
            <label htmlFor="firstName">👤 {t('first_name')}</label>
            <input
              type="text"
              id="firstName"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="lastName">👤 {t('last_name')}</label>
            <input
              type="text"
              id="lastName"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="email">✉️ {t('email')}</label>
            <input
              type="email"
              id="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="password">🔒 {t('password')}</label>
            <div className="password-input-wrapper">
              <input
                type="password"
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                onFocus={() => setShowPasswordHint(true)}
                onBlur={() => setShowPasswordHint(false)}
                required
                className={error && password.length < 8 ? 'input-error' : ''}
              />
              <button
                type="button"
                className="password-hint-icon"
                onMouseEnter={() => setShowPasswordHint(true)}
                onMouseLeave={() => setShowPasswordHint(false)}
                onClick={() => setShowPasswordHint(!showPasswordHint)}
              >
                ?
              </button>
              {showPasswordHint && (
                <div className="password-hint-tooltip">
                  {t('password_requirements')}
                </div>
              )}
            </div>
          </div>
          <button type="submit" className="register-button">
            ✅ {t('register')}
          </button>
          <div className="message-area">
            {error && <p className="error-message">⚠️ {error}</p>}
            {success && <p className="success-message">✅ {success}</p>}
          </div>
        </form>
        <div className="login-link">
          <p>
            {t('already_have_account')}? <Link to="/login">🔐 {t('login')}</Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
