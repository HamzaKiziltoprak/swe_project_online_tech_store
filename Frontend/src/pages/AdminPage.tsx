import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../lib/api';
import type { AdminStats, UserProfile, ProductSummary, Category, Brand } from '../lib/api';
import { useAuth } from '../context/AuthContext';
import '../styles/Admin.css';

const AdminPage: React.FC = () => {
  const { token } = useAuth();
  const { t } = useTranslation();
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [error, setError] = useState<string | null>(null);

  // User Management State
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [loadingUsers, setLoadingUsers] = useState<boolean>(true);
  const [userError, setUserError] = useState<string | null>(null);

  // Product Management State
  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [brands, setBrands] = useState<Brand[]>([]);
  const [loadingProducts, setLoadingProducts] = useState<boolean>(false);
  const [productError, setProductError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'users' | 'products'>('users');
  const [showProductForm, setShowProductForm] = useState(false);
  const [editingProductId, setEditingProductId] = useState<number | null>(null);
  const [formData, setFormData] = useState({
    productName: '',
    description: '',
    price: '',
    stock: '',
    categoryID: '',
    brandID: '',
    imageUrl: '',
    isActive: true,
  });

  const fetchUsersAndRoles = () => {
    if (!token) return;
    setLoadingUsers(true);
    setUserError(null);
    Promise.all([api.getAllUsers(token), api.getAllRoles(token)])
      .then(([userRes, roleRes]) => {
        setUsers(userRes);
        setRoles(roleRes);
      })
      .catch((err) => setUserError(err.message || t('users_and_roles_error')))
      .finally(() => setLoadingUsers(false));
  };

  const fetchProducts = async () => {
    if (!token) return;
    setLoadingProducts(true);
    setProductError(null);
    try {
      const res = await api.getAllProducts(token, 1, 100);
      setProducts(res.items);
    } catch (err: any) {
      setProductError(err.message || t('error_loading_products'));
    } finally {
      setLoadingProducts(false);
    }
  };

  const fetchCategoriesAndBrands = async () => {
    try {
      const [catRes, brandRes] = await Promise.all([
        api.getCategories(),
        api.getBrands(),
      ]);
      setCategories(catRes);
      setBrands(brandRes);
    } catch (err: any) {
      console.error('Error fetching categories/brands:', err);
    }
  };

  useEffect(() => {
    if (!token) return;
    api
      .getAdminStats(token)
      .then(setStats)
      .catch((err) => setError(err.message || t('admin_stats_error')));

    fetchUsersAndRoles();
    fetchCategoriesAndBrands();
  }, [token]);

  const handleProductSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    try {
      const payload = {
        productName: formData.productName,
        description: formData.description,
        price: Number(formData.price),
        stock: Number.parseInt(formData.stock, 10),
        categoryID: Number.parseInt(formData.categoryID, 10),
        brandID: Number.parseInt(formData.brandID, 10),
        imageUrl: formData.imageUrl,
        isActive: formData.isActive,
      };

      if (editingProductId) {
        await api.updateProduct(editingProductId, payload, token);
      } else {
        await api.createProduct(payload, token);
      }

      setFormData({
        productName: '',
        description: '',
        price: '',
        stock: '',
        categoryID: '',
        brandID: '',
        imageUrl: '',
        isActive: true,
      });
      setShowProductForm(false);
      setEditingProductId(null);
      fetchProducts();
    } catch (err: any) {
      setProductError(err.message || t('error_saving_product'));
    }
  };

  const handleDeleteProduct = async (id: number) => {
    if (!token || !window.confirm(t('confirm_delete'))) return;
    try {
      await api.deleteProduct(id, token);
      fetchProducts();
    } catch (err: any) {
      setProductError(err.message || t('error_deleting_product'));
    }
  };

  const handleEditProduct = async (product: ProductSummary) => {
    if (!token) return;
    setProductError(null);
    setEditingProductId(product.productID);
    setShowProductForm(true);
    try {
      const detail = await api.getProductDetail(product.productID);
      setFormData({
        productName: detail.productName,
        description: detail.description || '',
        price: detail.price.toString(),
        stock: detail.stock.toString(),
        categoryID: detail.categoryID ? String(detail.categoryID) : '',
        brandID: detail.brandID ? String(detail.brandID) : String(product.brandID || ''),
        imageUrl: detail.imageUrl || '',
        isActive: detail.isActive ?? true,
      });
    } catch (err: any) {
      setProductError(err.message || t('error_loading_product'));
    }
  };

  return (
    <div className="admin-page">
      <h2>{t('admin_title')}</h2>
      <p>{t('stats_title')}</p>
      {error && <p className="error">{error}</p>}
      {!error && !stats && <p>{t('loading')}</p>}
      {stats && (
        <div className="stats-grid">
          <div className="stat-card">
            <span>{t('products')}</span>
            <strong>{stats.totalProducts}</strong>
          </div>
          <div className="stat-card">
            <span>{t('active_products')}</span>
            <strong>{stats.activeProducts}</strong>
          </div>
          <div className="stat-card">
            <span>{t('out_of_stock')}</span>
            <strong>{stats.outOfStockProducts}</strong>
          </div>
          <div className="stat-card">
            <span>{t('categories')}</span>
            <strong>{stats.totalCategories}</strong>
          </div>
          <div className="stat-card">
            <span>{t('orders_title')}</span>
            <strong>{stats.totalOrders}</strong>
          </div>
          <div className="stat-card">
            <span>{t('pending_orders')}</span>
            <strong>{stats.pendingOrders}</strong>
          </div>
          <div className="stat-card">
            <span>{t('completed_orders')}</span>
            <strong>{stats.completedOrders}</strong>
          </div>
          <div className="stat-card">
            <span>{t('cancelled_orders')}</span>
            <strong>{stats.cancelledOrders}</strong>
          </div>
        </div>
      )}

      <div className="admin-tabs">
        <button
          className={`tab-button ${activeTab === 'users' ? 'active' : ''}`}
          onClick={() => setActiveTab('users')}
        >
          👥 {t('user_management_title')}
        </button>
        <button
          className={`tab-button ${activeTab === 'products' ? 'active' : ''}`}
          onClick={() => {
            setActiveTab('products');
            if (products.length === 0) {
              fetchProducts();
            }
          }}
        >
          📦 {t('product_management')}
        </button>
      </div>

      {activeTab === 'users' && (
        <section className="user-management panel">
          <h3>👥 {t('user_management_title')}</h3>
          {userError && <p className="error">⚠️ {userError}</p>}
          {loadingUsers && <p>⏳ {t('loading')}</p>}
          {!loadingUsers && (
            <div className="user-list">
              <table>
                <thead>
                  <tr>
                    <th>👤 {t('user')}</th>
                    <th>🔐 {t('roles')}</th>
                    <th>⚙️ {t('actions')}</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => (
                    <UserRow
                      key={u.id}
                      user={u}
                      allRoles={roles}
                      onRoleChange={fetchUsersAndRoles}
                    />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      {activeTab === 'products' && (
        <section className="product-management panel">
          <div className="product-header">
            <h3>📦 {t('product_management')}</h3>
            <button
              className="add-product-btn"
              onClick={() => {
                setShowProductForm(true);
                setEditingProductId(null);
                setFormData({
                  productName: '',
                  description: '',
                  price: '',
                  stock: '',
                  categoryID: '',
                  brandID: '',
                  imageUrl: '',
                  isActive: true,
                });
              }}
            >
              ➕ {t('add_new_product')}
            </button>
          </div>

          {productError && <p className="error">⚠️ {productError}</p>}

          {showProductForm && (
            <div className="modal-backdrop" onClick={() => {
              setShowProductForm(false);
              setEditingProductId(null);
            }}>
              <div className="modal-container" onClick={(e) => e.stopPropagation()}>
                <div className="modal-header">
                  <h3>{editingProductId ? `✏️ ${t('edit')} ${t('product_name')}` : `➕ ${t('add_new_product')}`}</h3>
                  <button
                    className="modal-close-btn"
                    onClick={() => {
                      setShowProductForm(false);
                      setEditingProductId(null);
                    }}
                  >
                    ✕
                  </button>
                </div>
                <form className="product-form" onSubmit={handleProductSubmit}>
                  <div className="form-row">
                    <div className="form-group">
                      <label>{t('product_name')} *</label>
                      <input
                        type="text"
                        required
                        value={formData.productName}
                        onChange={(e) =>
                          setFormData({ ...formData, productName: e.target.value })
                        }
                      />
                    </div>
                    <div className="form-group">
                      <label>{t('price')} *</label>
                      <input
                        type="number"
                        step="0.01"
                        required
                        value={formData.price}
                        onChange={(e) =>
                          setFormData({ ...formData, price: e.target.value })
                        }
                      />
                    </div>
                  </div>

                  <div className="form-row">
                    <div className="form-group">
                      <label>{t('stock')} *</label>
                      <input
                        type="number"
                        required
                        value={formData.stock}
                        onChange={(e) =>
                          setFormData({ ...formData, stock: e.target.value })
                        }
                      />
                    </div>
                    <div className="form-group">
                      <label>{t('category')} *</label>
                      <select
                        required
                        value={formData.categoryID}
                        onChange={(e) =>
                          setFormData({ ...formData, categoryID: e.target.value })
                        }
                      >
                        <option value="">{t('select_category')}</option>
                        {categories.map((cat) => (
                          <option key={cat.categoryID} value={cat.categoryID}>
                            {cat.categoryName}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  <div className="form-row">
                    <div className="form-group">
                      <label>{t('brand')} *</label>
                      <select
                        required
                        value={formData.brandID}
                        onChange={(e) =>
                          setFormData({ ...formData, brandID: e.target.value })
                        }
                      >
                        <option value="">{t('select_brand')}</option>
                        {brands.map((brand) => (
                          <option key={brand.brandID} value={brand.brandID}>
                            {brand.brandName}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="form-group">
                      <label>{t('image_url')}</label>
                      <input
                        type="url"
                        value={formData.imageUrl}
                        onChange={(e) =>
                          setFormData({ ...formData, imageUrl: e.target.value })
                        }
                      />
                    </div>
                  </div>

                  <div className="form-group">
                    <label>{t('description')} *</label>
                    <textarea
                      required
                      rows={4}
                      value={formData.description}
                      onChange={(e) =>
                        setFormData({ ...formData, description: e.target.value })
                      }
                    />
                  </div>

                  <div className="form-actions">
                    <button type="submit" className="btn-primary">
                      {editingProductId ? t('update') : t('create')}
                    </button>
                    <button
                      type="button"
                      className="btn-secondary"
                      onClick={() => {
                        setShowProductForm(false);
                        setEditingProductId(null);
                      }}
                    >
                      {t('cancel')}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {loadingProducts && <p>⏳ {t('loading')}</p>}
          {!loadingProducts && products.length > 0 && (
            <div className="products-table">
              <table>
                <thead>
                  <tr>
                    <th>🛍️ {t('product_name')}</th>
                    <th>📦 {t('category')}</th>
                    <th>🏷️ {t('brand')}</th>
                    <th>💰 {t('price')}</th>
                    <th>📍 {t('stock')}</th>
                    <th>⚙️ {t('actions')}</th>
                  </tr>
                </thead>
                <tbody>
                  {products.map((product) => (
                    <tr key={product.productID}>
                      <td>🛍️ {product.productName}</td>
                      <td>📦 {product.categoryName || '-'}</td>
                      <td>🏷️ {product.brand || '-'}</td>
                      <td>💰 ₺{product.price.toFixed(2)}</td>
                      <td>
                        <span
                          className={`stock-badge ${product.stock > 0 ? 'in-stock' : 'out-of-stock'
                            }`}
                        >
                          {product.stock > 0 ? '✅' : '❌'} {product.stock}
                        </span>
                      </td>
                      <td>
                        <div className="product-actions">
                          <button
                            className="edit-btn"
                            onClick={() => handleEditProduct(product)}
                          >
                            ✏️ {t('edit')}
                          </button>
                          <button
                            className="delete-btn"
                            onClick={() => handleDeleteProduct(product.productID)}
                          >
                            🗑️ {t('delete')}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {!loadingProducts && products.length === 0 && (
            <p className="empty-message">✨ {t('no_products')}</p>
          )}
        </section>
      )}
    </div>
  );
};

interface UserRowProps {
  user: UserProfile;
  allRoles: string[];
  onRoleChange: () => void;
}

const UserRow: React.FC<UserRowProps> = ({ user, allRoles, onRoleChange }) => {
  const { token } = useAuth();
  const { t } = useTranslation();
  const [selectedRole, setSelectedRole] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);

  const availableRoles = allRoles.filter((r) => !user.roles?.includes(r));

  useEffect(() => {
    if (availableRoles.length > 0) {
      setSelectedRole(availableRoles[0]);
    }
  }, [user]);

  const handleAssignRole = async () => {
    if (!token || !selectedRole || !user.id) return;
    setError(null);
    try {
      await api.assignRole({ userId: user.id, roleName: selectedRole }, token);
      onRoleChange();
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleRemoveRole = async (roleName: string) => {
    if (!token || !user.id) return;
    setError(null);
    try {
      await api.removeRole({ userId: user.id, roleName: roleName }, token);
      onRoleChange();
    } catch (err: any) {
      setError(err.message);
    }
  };

  return (
    <tr>
      <td>
        <div className="user-info">
          <span>
            {user.firstName} {user.lastName}
          </span>
          <small>{user.email}</small>
        </div>
      </td>
      <td>
        <div className="user-roles">
          {user.roles?.map((role) => (
            <span key={role} className="role-badge">
              {role}
              <button onClick={() => handleRemoveRole(role)}>×</button>
            </span>
          ))}
        </div>
      </td>
      <td>
        <div className="role-actions">
          {isEditing ? (
            <>
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                disabled={availableRoles.length === 0}
              >
                {availableRoles.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
              <button onClick={handleAssignRole} disabled={availableRoles.length === 0}>
                {t('add_role')}
              </button>
              <button onClick={() => setIsEditing(false)} className="cancel-btn">
                {t('cancel')}
              </button>
            </>
          ) : (
            <button className="edit-role-btn" onClick={() => setIsEditing(true)}>
              ✏️ {t('edit')}
            </button>
          )}
        </div>
        {error && <small className="error-message">{error}</small>}
      </td>
    </tr>
  );
};

export default AdminPage;
