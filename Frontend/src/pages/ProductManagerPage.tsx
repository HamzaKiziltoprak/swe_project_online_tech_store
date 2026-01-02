import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import ProductManagement from '../components/admin/ProductManagement';
import ReviewManagement from '../components/admin/ReviewManagement';
import '../styles/Admin.css';

const ProductManagerPage: React.FC = () => {
    const { token } = useAuth();
    const { t } = useTranslation();
    const [activeTab, setActiveTab] = useState<'products' | 'reviews'>('products');

    return (
        <div className="admin-page">
            <h2>📦 {t('product_manager_title') || 'Product Manager Panel'}</h2>
            <p>{t('product_manager_description') || 'Manage products and review moderation'}</p>

            <div className="admin-tabs">
                <button
                    className={`tab-button ${activeTab === 'products' ? 'active' : ''}`}
                    onClick={() => setActiveTab('products')}
                >
                    📦 {t('product_management')}
                </button>
                <button
                    className={`tab-button ${activeTab === 'reviews' ? 'active' : ''}`}
                    onClick={() => setActiveTab('reviews')}
                >
                    💬 {t('review_management')}
                </button>
            </div>

            {activeTab === 'products' && <ProductManagement token={token} />}
            {activeTab === 'reviews' && <ReviewManagement token={token} />}
        </div>
    );
};

export default ProductManagerPage;
