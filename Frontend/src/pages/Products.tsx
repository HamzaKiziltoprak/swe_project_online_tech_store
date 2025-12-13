import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api } from '../lib/api';
import type { Category, ProductSummary } from '../lib/api';
import { useAuth } from '../context/AuthContext';
import '../styles/Products.css';

interface FiltersState {
  searchTerm: string;
  brand: string;
  categoryId: string;
  minPrice: string;
  maxPrice: string;
  inStockOnly: boolean;
}

const defaultFilters: FiltersState = {
  searchTerm: '',
  brand: '',
  categoryId: '',
  minPrice: '',
  maxPrice: '',
  inStockOnly: false,
};

const Products = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { token } = useAuth();
  const [filters, setFilters] = useState<FiltersState>(defaultFilters);
  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [filterSearch, setFilterSearch] = useState<string>('');
  const [favoriteIds, setFavoriteIds] = useState<Set<number>>(new Set());

  const flattenCategories = (cats: Category[], depth = 0, list: Category[] = []) => {
    cats.forEach((cat) => {
      list.push({ ...cat, categoryName: `${'— '.repeat(depth)}${cat.categoryName}` });
      if (cat.subCategories && cat.subCategories.length > 0) {
        flattenCategories(cat.subCategories, depth + 1, list);
      }
    });
    return list;
  };

  useEffect(() => {
    api
      .getCategories()
      .then((data) => {
        setCategories(flattenCategories(data));
      })
      .catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    setLoading(true);
    setError(null);
    const query = {
      SearchTerm: filters.searchTerm,
      Brand: filters.brand,
      CategoryId: filters.categoryId ? Number(filters.categoryId) : undefined,
      MinPrice: filters.minPrice ? Number(filters.minPrice) : undefined,
      MaxPrice: filters.maxPrice ? Number(filters.maxPrice) : undefined,
      InStock: filters.inStockOnly ? true : undefined,
    };
    api
      .getProducts(query)
      .then((res) => setProducts(res.items))
      .catch((err) => setError(err.message || t('products_title')))
      .finally(() => setLoading(false));
  }, [filters, t]);

  useEffect(() => {
    if (!token) {
      setFavoriteIds(new Set());
      return;
    }
    api
      .getFavorites(token, 1, 200)
      .then((res) => setFavoriteIds(new Set(res.items.map((f: { productID: number }) => f.productID))))
      .catch(() => setFavoriteIds(new Set()));
  }, [token]);

  const activeFilters = useMemo(() => {
    const chips: string[] = [];
    if (filters.searchTerm) chips.push(`${t('search_label')}: ${filters.searchTerm}`);
    if (filters.brand) chips.push(`${t('brand_label')}: ${filters.brand}`);
    if (filters.categoryId) {
      const categoryName =
        categories.find((c) => String(c.categoryID) === filters.categoryId)?.categoryName ||
        t('category_label');
      chips.push(categoryName);
    }
    if (filters.minPrice) chips.push(`${t('price_min')} ${filters.minPrice}`);
    if (filters.maxPrice) chips.push(`${t('price_max')} ${filters.maxPrice}`);
    if (filters.inStockOnly) chips.push(t('stock_in'));
    return chips;
  }, [filters, categories, t]);

  const handleInputChange = (field: keyof FiltersState, value: string | boolean) => {
    setFilters((prev) => ({ ...prev, [field]: value }));
  };

  const clearFilters = () => setFilters(defaultFilters);

  const handleAddToCart = async (productId: number) => {
    if (!token) {
      navigate('/login');
      return;
    }
    try {
      await api.addToCart(productId, 1, token);
      alert(t('add_to_cart'));
    } catch (err: any) {
      alert(err.message || t('add_to_cart'));
    }
  };

  const handleFavorite = async (productId: number) => {
    if (!token) {
      navigate('/login');
      return;
    }
    try {
      const res = await api.toggleFavorite(productId, token);
      setFavoriteIds((prev) => {
        const next = new Set(prev);
        if (next.has(productId)) next.delete(productId);
        else next.add(productId);
        return next;
      });
      alert(res.message || t('favorite'));
    } catch (err: any) {
      alert(err.message || t('favorite_error'));
    }
  };

  return (
    <div className="products-page-container">
      <aside className="filter-bar">
        <h3>{t('filters_title')}</h3>
        <div className="filter-group">
          <label>{t('filter_search')}</label>
          <input
            type="text"
            value={filterSearch}
            onChange={(e) => setFilterSearch(e.target.value.toLowerCase())}
            placeholder={t('filter_search_placeholder')}
            className="filter-input"
          />
        </div>

        {(filterSearch === '' || 'search arama'.includes(filterSearch)) && (
          <div className="filter-group">
            <label>{t('search_label')}</label>
            <input
              type="text"
              value={filters.searchTerm}
              onChange={(e) => handleInputChange('searchTerm', e.target.value)}
              placeholder={t('search_placeholder')}
              className="filter-input"
            />
          </div>
        )}

        {(filterSearch === '' || 'brand marka'.includes(filterSearch)) && (
          <div className="filter-group">
            <label>{t('brand_label')}</label>
            <input
              type="text"
              value={filters.brand}
              onChange={(e) => handleInputChange('brand', e.target.value)}
              placeholder={t('brand_placeholder')}
              className="filter-input"
            />
          </div>
        )}

        {(filterSearch === '' || 'category kategori'.includes(filterSearch)) && (
          <div className="filter-group">
            <label>{t('category_label')}</label>
            <select
              value={filters.categoryId}
              onChange={(e) => handleInputChange('categoryId', e.target.value)}
              className="filter-input"
            >
              <option value="">{t('category_all')}</option>
              {categories.map((cat) => (
                <option key={cat.categoryID} value={cat.categoryID}>
                  {cat.categoryName}
                </option>
              ))}
            </select>
          </div>
        )}

        {(filterSearch === '' || 'price fiyat'.includes(filterSearch)) && (
          <div className="filter-row">
            <div className="filter-group">
              <label>{t('price_min')}</label>
              <input
                type="number"
                value={filters.minPrice}
                onChange={(e) => handleInputChange('minPrice', e.target.value)}
                className="filter-input"
              />
            </div>
            <div className="filter-group">
              <label>{t('price_max')}</label>
              <input
                type="number"
                value={filters.maxPrice}
                onChange={(e) => handleInputChange('maxPrice', e.target.value)}
                className="filter-input"
              />
            </div>
          </div>
        )}
        {(filterSearch === '' || 'stock stok'.includes(filterSearch)) && (
          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={filters.inStockOnly}
              onChange={(e) => handleInputChange('inStockOnly', e.target.checked)}
            />
            {t('in_stock_only')}
          </label>
        )}
        <button className="clear-button" onClick={clearFilters}>
          {t('clear_filters')}
        </button>
      </aside>

      <main className="products-list">
        <div className="view-options">
          <h2>{`${t('products_title')} (${products.length})`}</h2>
          <div className="view-buttons">
            <button onClick={() => setViewMode('grid')} disabled={viewMode === 'grid'}>
              {t('grid')}
            </button>
            <button onClick={() => setViewMode('list')} disabled={viewMode === 'list'}>
              {t('list')}
            </button>
          </div>
        </div>

        <div className="chips">
          {activeFilters.map((chip) => (
            <span key={chip} className="chip">
              {chip}
            </span>
          ))}
        </div>

        {loading && <p>{t('loading')}</p>}
        {error && <p className="error">{error}</p>}

        {!loading && !products.length && <p>{t('no_products')}</p>}

        <div className={viewMode === 'grid' ? 'products-grid' : 'products-list-view'}>
          {products.map((product) => (
            <div key={product.productID} className="product-card">
              <img
                src={product.imageUrl}
                alt={product.productName}
                className="product-image"
              />
              <div className="product-details">
                <div className="product-header">
                  <h3 className="product-name">{product.productName}</h3>
                  <span className="brand-pill">{product.brand}</span>
                </div>
                <p className="product-price">₺{product.price}</p>
                <p className="stock">
                  {product.stock > 0 ? t('stock_in') : t('stock_out')}
                </p>
                <div className="product-actions">
                  <Link to={`/products/${product.productID}`} className="link-button">
                    {t('details')}
                  </Link>
                  <button onClick={() => handleAddToCart(product.productID)}>{t('add_to_cart')}</button>
                  <button onClick={() => handleFavorite(product.productID)}>
                    {favoriteIds.has(product.productID) ? t('favorite_removed') : t('favorite')}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </main>
    </div>
  );
};

export default Products;
