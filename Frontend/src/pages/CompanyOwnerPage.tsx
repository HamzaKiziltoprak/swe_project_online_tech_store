import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { api } from '../lib/api';
import type { AdminStats, Order, ProductSummary, Review } from '../lib/api';
import {
    AreaChart,
    Area,
    PieChart,
    Pie,
    Cell,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    ResponsiveContainer,
    Legend,
} from 'recharts';
import '../styles/CompanyOwner.css';

interface DashboardData {
    stats: AdminStats | null;
    orders: Order[];
    products: ProductSummary[];
    reviews: Review[];
    mostViewedProducts: ProductSummary[];
    mostFavoritedProducts: any[];
}

const CompanyOwnerPage: React.FC = () => {
    const { token, user } = useAuth();
    const { t } = useTranslation();

    // DEBUG: Log user info
    console.log('CompanyOwner Debug - User:', user);
    console.log('CompanyOwner Debug - Token exists:', !!token);
    console.log('CompanyOwner Debug - Roles:', user?.roles);

    const [data, setData] = useState<DashboardData>({
        stats: null,
        orders: [],
        products: [],
        reviews: [],
        mostViewedProducts: [],
        mostFavoritedProducts: [],
    });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!token) return;

        const fetchDashboardData = async () => {
            try {
                setLoading(true);

                // Fetch data in parallel with individual error handling and logging
                const [statsRes, ordersRes, productsRes, reviewsRes, mostViewedRes, mostFavoritedRes] = await Promise.all([
                    api.getAdminStats(token).catch((e) => { console.error('getAdminStats error:', e); return null; }),
                    api.getAllOrdersForOwner(token, 1, 1000).catch((e) => { console.error('getAllOrdersForOwner error:', e); return { items: [] }; }),
                    api.getProducts({ PageSize: 100 }).catch((e) => { console.error('getProducts error:', e); return { items: [] }; }),
                    api.getLatestReviews(token, 5).catch((e) => { console.error('getLatestReviews error:', e); return []; }),
                    api.getMostViewedProducts(token, 5).catch((e) => { console.error('getMostViewedProducts error:', e); return []; }),
                    api.getMostFavoritedProducts(token, 5).catch((e) => { console.error('getMostFavoritedProducts error:', e); return []; }),
                ]);

                console.log('Dashboard Data:', { statsRes, ordersRes, productsRes, reviewsRes, mostViewedRes, mostFavoritedRes });
                console.log('Orders count:', ordersRes?.items?.length || 0);
                console.log('Products count:', productsRes?.items?.length || 0);
                console.log('Stats:', statsRes);

                setData({
                    stats: statsRes,
                    orders: ordersRes?.items || [],
                    products: productsRes?.items || [],
                    reviews: reviewsRes || [],
                    mostViewedProducts: mostViewedRes || [],
                    mostFavoritedProducts: mostFavoritedRes || [],
                });
                setError(null);
            } catch (err: any) {
                setError(err.message || t('admin_stats_error'));
            } finally {
                setLoading(false);
            }
        };

        fetchDashboardData();
    }, [token, t]);

    // Check if user has required role
    const hasRequiredRole = user?.roles?.includes('CompanyOwner') || user?.roles?.includes('Admin');
    if (!hasRequiredRole && user) {
        return (
            <div className="company-owner-page">
                <h2>⚠️ Yetki Hatası</h2>
                <p>Bu sayfayı görüntülemek için <strong>CompanyOwner</strong> veya <strong>Admin</strong> rolüne ihtiyacınız var.</p>
                <p>Mevcut rolleriniz: <strong>{user?.roles?.join(', ') || 'Rol yok'}</strong></p>
                <p>Lütfen bir Admin'den size CompanyOwner rolü atamasını isteyin.</p>
            </div>
        );
    }

    // Calculate daily sales metrics
    const getDailySales = () => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        const todayOrders = data.orders.filter((order) => {
            const orderDate = new Date(order.orderDate);
            orderDate.setHours(0, 0, 0, 0);
            return orderDate.getTime() === today.getTime();
        });

        return {
            count: todayOrders.length,
            revenue: todayOrders.reduce((sum, order) => sum + order.totalAmount, 0),
        };
    };

    // Generate revenue chart data (last 30 days)
    const getRevenueChartData = () => {
        const days = 30;
        const chartData = [];
        const today = new Date();

        for (let i = days - 1; i >= 0; i--) {
            const date = new Date(today);
            date.setDate(date.getDate() - i);
            date.setHours(0, 0, 0, 0);

            const dayOrders = data.orders.filter((order) => {
                const orderDate = new Date(order.orderDate);
                orderDate.setHours(0, 0, 0, 0);
                return orderDate.getTime() === date.getTime();
            });

            const revenue = dayOrders.reduce((sum, order) => sum + order.totalAmount, 0);

            chartData.push({
                date: `${date.getDate()}/${date.getMonth() + 1}`,
                revenue: revenue,
                orders: dayOrders.length,
            });
        }

        return chartData;
    };

    // Generate category distribution data
    const getCategoryDistribution = () => {
        const categoryMap = new Map<string, number>();

        data.products.forEach((product) => {
            const category = product.categoryName || t('category_all');
            categoryMap.set(category, (categoryMap.get(category) || 0) + product.stock);
        });

        return Array.from(categoryMap.entries()).map(([name, value]) => ({
            name,
            value,
        }));
    };

    // Get low stock products
    const getLowStockProducts = () => {
        return data.products
            .filter((p) => p.stock < 10)
            .sort((a, b) => a.stock - b.stock)
            .slice(0, 5);
    };

    // Get recent orders
    const getRecentOrders = () => {
        return [...data.orders]
            .sort((a, b) => new Date(b.orderDate).getTime() - new Date(a.orderDate).getTime())
            .slice(0, 5);
    };

    // Get latest reviews
    const getLatestReviews = () => {
        return [...data.reviews]
            .sort((a, b) => new Date(b.reviewDate).getTime() - new Date(a.reviewDate).getTime())
            .slice(0, 5);
    };

    // Get top selling products (based on order items)
    const getTopSellingProducts = () => {
        const productSales = new Map<number, { name: string; quantity: number; revenue: number }>();

        // Aggregate sales data from all orders
        data.orders.forEach((order) => {
            order.items?.forEach((item) => {
                const existing = productSales.get(item.productID) || {
                    name: item.productName,
                    quantity: 0,
                    revenue: 0,
                };
                productSales.set(item.productID, {
                    name: item.productName,
                    quantity: existing.quantity + item.quantity,
                    revenue: existing.revenue + item.subtotal,
                });
            });
        });

        // Convert to array and sort by quantity sold
        return Array.from(productSales.entries())
            .map(([productID, data]) => ({
                productID,
                productName: data.name,
                totalSold: data.quantity,
                totalRevenue: data.revenue,
            }))
            .sort((a, b) => b.totalSold - a.totalSold)
            .slice(0, 5);
    };

    if (loading) {
        return (
            <div className="company-owner-page">
                <h2>{t('company_owner_dashboard')}</h2>
                <p>{t('loading')}</p>
            </div>
        );
    }

    if (error) {
        return (
            <div className="company-owner-page">
                <h2>{t('company_owner_dashboard')}</h2>
                <p className="error">{error}</p>
            </div>
        );
    }

    const dailySales = getDailySales();
    const revenueData = getRevenueChartData();
    const categoryData = getCategoryDistribution();
    const lowStockProducts = getLowStockProducts();
    const recentOrders = getRecentOrders();
    const latestReviews = getLatestReviews();
    const topSellingProducts = getTopSellingProducts();

    const totalStock = data.products.reduce((sum, p) => sum + p.stock, 0);
    const activeProducts = data.products.filter((p) => p.isActive !== false).length;

    const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D'];

    return (
        <div className="company-owner-page">
            <h2>{t('company_owner_dashboard')}</h2>
            <p className="dashboard-subtitle">{t('company_owner_subtitle')}</p>

            {/* KPI Cards Section */}
            <div className="kpi-grid">
                <div className="kpi-card">
                    <div className="kpi-icon">📊</div>
                    <div className="kpi-content">
                        <h3>{dailySales.count}</h3>
                        <p>{t('daily_sales_count')}</p>
                    </div>
                </div>

                <div className="kpi-card">
                    <div className="kpi-icon">💰</div>
                    <div className="kpi-content">
                        <h3>₺{dailySales.revenue.toFixed(2)}</h3>
                        <p>{t('daily_revenue')}</p>
                    </div>
                </div>

                <div className="kpi-card">
                    <div className="kpi-icon">📦</div>
                    <div className="kpi-content">
                        <h3>{totalStock}</h3>
                        <p>{t('total_stock')}</p>
                    </div>
                </div>

                <div className="kpi-card">
                    <div className="kpi-icon">✅</div>
                    <div className="kpi-content">
                        <h3>{activeProducts}</h3>
                        <p>{t('active_products_count')}</p>
                    </div>
                </div>
            </div>

            {/* Charts Section */}
            <div className="charts-section">
                <div className="chart-card">
                    <h3>{t('revenue_trend')}</h3>
                    <ResponsiveContainer width="100%" height={300}>
                        <AreaChart data={revenueData}>
                            <defs>
                                <linearGradient id="colorRevenue" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor="#8884d8" stopOpacity={0.8} />
                                    <stop offset="95%" stopColor="#8884d8" stopOpacity={0} />
                                </linearGradient>
                            </defs>
                            <CartesianGrid strokeDasharray="3 3" />
                            <XAxis dataKey="date" />
                            <YAxis />
                            <Tooltip />
                            <Legend />
                            <Area
                                type="monotone"
                                dataKey="revenue"
                                stroke="#8884d8"
                                fillOpacity={1}
                                fill="url(#colorRevenue)"
                                name={t('revenue')}
                            />
                        </AreaChart>
                    </ResponsiveContainer>
                </div>

                <div className="chart-card">
                    <h3>{t('category_distribution')}</h3>
                    <ResponsiveContainer width="100%" height={300}>
                        <PieChart>
                            <Pie
                                data={categoryData}
                                cx="50%"
                                cy="50%"
                                labelLine={false}
                                label={({ name, percent }) => `${name}: ${((percent ?? 0) * 100).toFixed(0)}%`}
                                outerRadius={80}
                                fill="#8884d8"
                                dataKey="value"
                            >
                                {categoryData.map((entry, index) => (
                                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                                ))}
                            </Pie>
                            <Tooltip />
                        </PieChart>
                    </ResponsiveContainer>
                </div>
            </div>

            {/* Activity Panels Section */}
            <div className="activity-grid">
                {/* Recent Orders */}
                <div className="activity-panel">
                    <h3>{t('recent_orders')}</h3>
                    {recentOrders.length === 0 ? (
                        <p className="empty-message">{t('no_recent_orders')}</p>
                    ) : (
                        <ul className="activity-list">
                            {recentOrders.map((order) => (
                                <li key={order.orderID} className="activity-item">
                                    <div className="activity-info">
                                        <strong>{order.userEmail}</strong>
                                        <span className="activity-meta">
                                            ₺{order.totalAmount.toFixed(2)} • {new Date(order.orderDate).toLocaleDateString('tr-TR')}
                                        </span>
                                    </div>
                                    <span className={`status-badge status-${order.status.toLowerCase()}`}>
                                        {order.status}
                                    </span>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* Latest Reviews */}
                <div className="activity-panel">
                    <h3>{t('latest_reviews')}</h3>
                    {latestReviews.length === 0 ? (
                        <p className="empty-message">{t('no_pending_reviews')}</p>
                    ) : (
                        <ul className="activity-list">
                            {latestReviews.map((review) => (
                                <li key={review.productReviewID} className="activity-item">
                                    <div className="activity-info">
                                        <strong>{review.productName || t('product')}</strong>
                                        <div className="review-rating">
                                            {'⭐'.repeat(review.rating)}
                                            <span className="rating-number">({review.rating}/5)</span>
                                        </div>
                                        {review.reviewText && (
                                            <p className="review-text">{review.reviewText.substring(0, 50)}...</p>
                                        )}
                                    </div>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* Stock Alert */}
                <div className="activity-panel stock-alert-panel">
                    <h3>{t('stock_alert')}</h3>
                    {lowStockProducts.length === 0 ? (
                        <p className="empty-message success">{t('no_stock_alerts')}</p>
                    ) : (
                        <ul className="activity-list">
                            {lowStockProducts.map((product) => (
                                <li key={product.productID} className="activity-item alert-item">
                                    <div className="activity-info">
                                        <strong>{product.productName}</strong>
                                        <span className="stock-warning">
                                            {t('remaining_stock')}: <strong>{product.stock}</strong>
                                        </span>
                                    </div>
                                    <span className="alert-icon">⚠️</span>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* Top Selling Products */}
                <div className="activity-panel top-products-panel">
                    <h3>{t('top_selling_products')}</h3>
                    {topSellingProducts.length === 0 ? (
                        <p className="empty-message">{t('no_sales_data')}</p>
                    ) : (
                        <ul className="activity-list">
                            {topSellingProducts.map((product, index) => (
                                <li key={product.productID} className="activity-item top-product-item">
                                    <div className="rank-badge">{index + 1}</div>
                                    <div className="activity-info">
                                        <strong>{product.productName}</strong>
                                        <div className="product-stats">
                                            <span className="stat-item">
                                                📦 {product.totalSold} {t('units_sold')}
                                            </span>
                                            <span className="stat-item revenue">
                                                💰 ₺{product.totalRevenue.toFixed(2)}
                                            </span>
                                        </div>
                                    </div>
                                    {index === 0 && <span className="trophy-icon">🏆</span>}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* Most Viewed Products */}
                <div className="activity-panel most-viewed-panel">
                    <h3>{t('most_viewed_products')}</h3>
                    {data.mostViewedProducts.length === 0 ? (
                        <p className="empty-message">{t('no_view_data')}</p>
                    ) : (
                        <ul className="activity-list">
                            {data.mostViewedProducts.map((product, index) => (
                                <li key={product.productID} className="activity-item most-viewed-item">
                                    <div className="rank-badge view-badge">{index + 1}</div>
                                    <div className="activity-info">
                                        <strong>{product.productName}</strong>
                                        <div className="product-stats">
                                            <span className="stat-item views">
                                                👁️ {product.viewCount || 0} {t('views')}
                                            </span>
                                            <span className="stat-item">
                                                💰 ₺{product.price.toFixed(2)}
                                            </span>
                                        </div>
                                    </div>
                                    {index === 0 && <span className="eye-icon">👁️</span>}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* Most Favorited Products */}
                <div className="activity-panel most-favorited-panel">
                    <h3>{t('most_favorited_products')}</h3>
                    {data.mostFavoritedProducts.length === 0 ? (
                        <p className="empty-message">{t('no_favorites_data')}</p>
                    ) : (
                        <ul className="activity-list">
                            {data.mostFavoritedProducts.map((product, index) => (
                                <li key={product.productID} className="activity-item most-favorited-item">
                                    <div className="rank-badge favorite-badge">{index + 1}</div>
                                    <div className="activity-info">
                                        <strong>{product.productName}</strong>
                                        <div className="product-stats">
                                            <span className="stat-item favorites">
                                                ❤️ {product.favoriteCount} {t('favorites_count')}
                                            </span>
                                            <span className="stat-item">
                                                💰 ₺{product.price?.toFixed(2) || '0.00'}
                                            </span>
                                        </div>
                                    </div>
                                    {index === 0 && <span className="heart-icon">❤️</span>}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* Sold Products Table */}
                <div className="sold-products-section">
                    <h3>🧾 {t('sold_products_table')}</h3>
                    {data.orders.length === 0 ? (
                        <p className="empty-message">{t('no_sales_data')}</p>
                    ) : (
                        <div className="sold-products-table-wrapper">
                            <table className="sold-products-table">
                                <thead>
                                    <tr>
                                        <th>{t('order_id')}</th>
                                        <th>{t('product')}</th>
                                        <th>{t('customer')}</th>
                                        <th>{t('quantity')}</th>
                                        <th>{t('price')}</th>
                                        <th>{t('total')}</th>
                                        <th>{t('date')}</th>
                                        <th>{t('status')}</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {data.orders
                                        .flatMap((order) =>
                                            order.items?.map((item) => ({
                                                orderID: order.orderID,
                                                productName: item.productName,
                                                customerEmail: order.userEmail,
                                                quantity: item.quantity,
                                                unitPrice: item.unitPrice,
                                                subtotal: item.subtotal,
                                                orderDate: order.orderDate,
                                                status: order.status,
                                            })) || []
                                        )
                                        .sort((a, b) => new Date(b.orderDate).getTime() - new Date(a.orderDate).getTime())
                                        .slice(0, 50)
                                        .map((sale, index) => (
                                            <tr key={`${sale.orderID}-${index}`}>
                                                <td>#{sale.orderID}</td>
                                                <td>{sale.productName}</td>
                                                <td>{sale.customerEmail}</td>
                                                <td>{sale.quantity}</td>
                                                <td>₺{sale.unitPrice.toFixed(2)}</td>
                                                <td>₺{sale.subtotal.toFixed(2)}</td>
                                                <td>{new Date(sale.orderDate).toLocaleDateString('tr-TR')}</td>
                                                <td>
                                                    <span className={`status-badge status-${sale.status.toLowerCase()}`}>
                                                        {sale.status}
                                                    </span>
                                                </td>
                                            </tr>
                                        ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default CompanyOwnerPage;
