import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../../lib/api';
import type { ProductSummary, Category, Brand } from '../../lib/api';

interface ProductManagementProps {
    token: string | null;
}

const ProductManagement: React.FC<ProductManagementProps> = ({ token }) => {
    const { t } = useTranslation();
    const [products, setProducts] = useState<ProductSummary[]>([]);
    const [categories, setCategories] = useState<Category[]>([]);
    const [brands, setBrands] = useState<Brand[]>([]);
    const [loading, setLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);
    const [showForm, setShowForm] = useState(false);
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

    const fetchProducts = async () => {
        if (!token) return;
        setLoading(true);
        setError(null);
        try {
            const res = await api.getAllProducts(token, 1, 100);
            setProducts(res.items);
        } catch (err: any) {
            setError(err.message || t('error_loading_products'));
        } finally {
            setLoading(false);
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
        fetchProducts();
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
            setShowForm(false);
            setEditingProductId(null);
            fetchProducts();
        } catch (err: any) {
            setError(err.message || t('error_saving_product'));
        }
    };

    const handleDeleteProduct = async (id: number) => {
        if (!token || !window.confirm(t('confirm_delete'))) return;
        try {
            await api.deleteProduct(id, token);
            fetchProducts();
        } catch (err: any) {
            setError(err.message || t('error_deleting_product'));
        }
    };

    const handleEditProduct = async (product: ProductSummary) => {
        if (!token) return;
        setError(null);
        setEditingProductId(product.productID);
        setShowForm(true);
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
            setError(err.message || t('error_loading_product'));
        }
    };

    return (
        <section className="product-management panel">
            <div className="product-header">
                <h3>📦 {t('product_management')}</h3>
                <button
                    className="add-product-btn"
                    onClick={() => {
                        setShowForm(true);
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

            {error && <p className="error">⚠️ {error}</p>}

            {showForm && (
                <div className="modal-backdrop" onClick={() => {
                    setShowForm(false);
                    setEditingProductId(null);
                }}>
                    <div className="modal-container" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>{editingProductId ? `✏️ ${t('edit')} ${t('product_name')}` : `➕ ${t('add_new_product')}`}</h3>
                            <button
                                className="modal-close-btn"
                                onClick={() => {
                                    setShowForm(false);
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
                                        setShowForm(false);
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

            {loading && <p>⏳ {t('loading')}</p>}
            {!loading && products.length > 0 && (
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
            {!loading && products.length === 0 && (
                <p className="empty-message">✨ {t('no_products')}</p>
            )}
        </section>
    );
};

export default ProductManagement;
