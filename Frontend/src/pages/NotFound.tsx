import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import '../styles/NotFound.css';

const NotFound: React.FC = () => {
  const { t } = useTranslation();

  return (
    <div className="not-found-container">
      <div className="not-found-content">
        <h1 className="error-code">🚫 404</h1>
        <h2 className="error-title">❌ {t('page_not_found')}</h2>
        <p className="error-message">📍 {t('page_not_found_message')}</p>
        
        <div className="error-actions">
          <Link to="/products" className="btn-home">
            🛍️ {t('back_to_products')}
          </Link>
          <Link to="/" className="btn-home secondary">
            🏠 {t('back_to_home')}
          </Link>
        </div>
      </div>
    </div>
  );
};

export default NotFound;
