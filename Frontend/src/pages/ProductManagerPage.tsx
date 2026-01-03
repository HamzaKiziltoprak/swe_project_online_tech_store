import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'react-toastify';
import { useAuth } from '../context/AuthContext';
import { api } from '../lib/api';
import type { Order } from '../lib/api';
import ProductManagement from '../components/admin/ProductManagement';
import ReviewManagement from '../components/admin/ReviewManagement';
import '../styles/Admin.css';

const ProductManagerPage: React.FC = () => {
    const { token } = useAuth();
    const { t } = useTranslation();
    const [activeTab, setActiveTab] = useState<'products' | 'reviews' | 'orders'>('products');
    const [orders, setOrders] = useState<Order[]>([]);
    const [loadingOrders, setLoadingOrders] = useState(false);
    const [updatingOrderId, setUpdatingOrderId] = useState<number | null>(null);

    useEffect(() => {
        if (activeTab === 'orders' && token) {
            loadOrders();
        }
    }, [activeTab, token]);

    const loadOrders = async () => {
        if (!token) return;
        setLoadingOrders(true);
        try {
            const result = await api.getAllOrdersForOwner(token, 1, 100);
            setOrders(result.items);
        } catch (err: any) {
            toast.error(err.message || t('fetch_error'));
        } finally {
            setLoadingOrders(false);
        }
    };

    const updateOrderStatus = async (orderId: number, newStatus: string) => {
        if (!token) return;
        setUpdatingOrderId(orderId);
        try {
            await api.updateOrderStatus(orderId, newStatus, token);
            setOrders(prev =>
                prev.map(order =>
                    order.orderID === orderId ? { ...order, status: newStatus } : order
                )
            );
            toast.success(t('status_updated'));
        } catch (err: any) {
            toast.error(err.message || t('status_update_error'));
        } finally {
            setUpdatingOrderId(null);
        }
    };

    return (
        <div className="admin-page">
            <h2>📦 {t('product_manager_title') || 'Product Manager Panel'}</h2>
            <p>{t('product_manager_description') || 'Manage products, reviews and orders'}</p>

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
                <button
                    className={`tab-button ${activeTab === 'orders' ? 'active' : ''}`}
                    onClick={() => setActiveTab('orders')}
                >
                    📋 {t('order_management')}
                </button>
            </div>

            {activeTab === 'products' && <ProductManagement token={token} />}
            {activeTab === 'reviews' && <ReviewManagement token={token} />}
            {activeTab === 'orders' && (
                <div className="order-management-section">
                    <h3>📋 {t('order_management')}</h3>
                    {loadingOrders ? (
                        <p>{t('loading')}</p>
                    ) : orders.length === 0 ? (
                        <p className="empty-message">{t('no_recent_orders')}</p>
                    ) : (
                        <div className="order-management-table-wrapper">
                            <table className="order-management-table">
                                <thead>
                                    <tr>
                                        <th>{t('order_id')}</th>
                                        <th>{t('customer')}</th>
                                        <th>{t('total')}</th>
                                        <th>{t('date')}</th>
                                        <th>{t('status')}</th>
                                        <th>{t('update_status')}</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {orders
                                        .sort((a, b) => new Date(b.orderDate).getTime() - new Date(a.orderDate).getTime())
                                        .map((order) => {
                                            const statusKey = `order_status_${order.status.toLowerCase()}`;
                                            const translatedStatus = t(statusKey) || order.status;
                                            return (
                                                <tr key={order.orderID}>
                                                    <td><strong>#{order.orderID}</strong></td>
                                                    <td>{order.userEmail}</td>
                                                    <td>₺{order.totalAmount.toFixed(2)}</td>
                                                    <td>{new Date(order.orderDate).toLocaleDateString('tr-TR')}</td>
                                                    <td>
                                                        <span className={`status-badge status-${order.status.toLowerCase()}`}>
                                                            {translatedStatus}
                                                        </span>
                                                    </td>
                                                    <td>
                                                        <select
                                                            className="status-select"
                                                            value={order.status}
                                                            onChange={(e) => updateOrderStatus(order.orderID, e.target.value)}
                                                            disabled={updatingOrderId === order.orderID}
                                                        >
                                                            <option value="Pending">{t('order_status_pending')}</option>
                                                            <option value="Processing">{t('order_status_processing')}</option>
                                                            <option value="Shipped">{t('order_status_shipped')}</option>
                                                            <option value="Delivered">{t('order_status_delivered')}</option>
                                                            <option value="Cancelled">{t('order_status_cancelled')}</option>
                                                        </select>
                                                        {updatingOrderId === order.orderID && <span className="updating-indicator">⏳</span>}
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export default ProductManagerPage;
