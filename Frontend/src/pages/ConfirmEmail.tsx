import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import '../styles/ConfirmEmail.css';

const ConfirmEmail = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [message, setMessage] = useState('');

  useEffect(() => {
    const confirmEmail = async () => {
      const userId = searchParams.get('userId');
      const token = searchParams.get('token');

      if (!userId || !token) {
        setStatus('error');
        setMessage('Invalid confirmation link. Missing parameters.');
        return;
      }

      try {
        const apiBase = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7100';
        // Token zaten URL-encoded geldiği için tekrar encode etmiyoruz
        const response = await fetch(
          `${apiBase}/api/accounts/confirm-email?userId=${userId}&token=${token}`,
          {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
          }
        );

        const data = await response.json();

        if (response.ok && data.success) {
          setStatus('success');
          setMessage(data.message || 'Email confirmed successfully!');
          
          // Redirect to login after 3 seconds
          setTimeout(() => {
            navigate('/login');
          }, 3000);
        } else {
          setStatus('error');
          setMessage(data.message || 'Email confirmation failed. Please try again.');
        }
      } catch (error) {
        setStatus('error');
        setMessage('An error occurred. Please try again later.');
        console.error('Email confirmation error:', error);
      }
    };

    confirmEmail();
  }, [searchParams, navigate]);

  return (
    <div className="confirm-email-container">
      <div className="confirm-email-card">
        {status === 'loading' && (
          <div className="loading-state">
            <div className="spinner"></div>
            <h2>{t('Confirming your email...')}</h2>
            <p>{t('Please wait...')}</p>
          </div>
        )}

        {status === 'success' && (
          <div className="success-state">
            <div className="success-icon">✓</div>
            <h2>{t('Email Confirmed!')}</h2>
            <p>{message}</p>
            <p className="redirect-message">{t('Redirecting to login page...')}</p>
            <button onClick={() => navigate('/login')} className="btn-primary">
              {t('Go to Login')}
            </button>
          </div>
        )}

        {status === 'error' && (
          <div className="error-state">
            <div className="error-icon">✕</div>
            <h2>{t('Confirmation Failed')}</h2>
            <p>{message}</p>
            <div className="error-actions">
              <button onClick={() => navigate('/login')} className="btn-secondary">
                {t('Go to Login')}
              </button>
              <button onClick={() => navigate('/register')} className="btn-primary">
                {t('Register Again')}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ConfirmEmail;
