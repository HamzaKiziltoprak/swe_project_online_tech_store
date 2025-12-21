import { useEffect, useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api } from '../lib/api';
import type {
  ProductDetail as ProductDetailType,
  ProductSpecification,
  ProductSummary,
  Review,
} from '../lib/api';
import { useAuth } from '../context/AuthContext';
import '../styles/ProductDetail.css';

const ProductDetail = () => {
  const { id } = useParams<{ id: string }>();
  const productId = Number(id);
  const navigate = useNavigate();
  const { token } = useAuth();
  const { t } = useTranslation();
  const [product, setProduct] = useState<ProductDetailType | null>(null);
  const [specs, setSpecs] = useState<ProductSpecification[]>([]);
  const [related, setRelated] = useState<ProductSummary[]>([]);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [reviewText, setReviewText] = useState('');
  const [rating, setRating] = useState(5);
  const [isFavorite, setIsFavorite] = useState(false);
  const [favoriteError, setFavoriteError] = useState<string | null>(null);

  useEffect(() => {
    if (!productId) return;
    const load = async () => {
      setLoading(true);
      try {
        const [detail, specList, relatedProducts, reviewList] = await Promise.all([
          api.getProductDetail(productId),
          api.getProductSpecifications(productId),
          api.getRelatedProducts(productId),
          api.getReviews(productId),
        ]);
        setProduct(detail);
        setSpecs(specList);
        setRelated(relatedProducts);
        setReviews(Array.isArray(reviewList) ? reviewList : []);
        setError(null);
      } catch (err: any) {
        setError(err.message || t('error_loading_product'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [productId, t]);

  useEffect(() => {
    if (!token || !productId) {
      setIsFavorite(false);
      return;
    }
    const checkFavorite = async () => {
      try {
        const fav = await api.isFavorite(productId, token);
        setIsFavorite(fav);
        setFavoriteError(null);
      } catch (err: any) {
        setFavoriteError(err.message || 'Favori kontrol hatası');
      }
    };
    checkFavorite();
  }, [productId, token]);

  const handleAddToCart = async () => {
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

  const handleFavorite = async () => {
    if (!token) {
      navigate('/login');
      return;
    }
    try {
      const res = await api.toggleFavorite(productId, token);
      setIsFavorite((prev) => !prev);
      alert(res.message || t('favorite'));
    } catch (err: any) {
      alert(err.message || t('favorite'));
    }
  };

  const submitReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) {
      navigate('/login');
      return;
    }
    try {
      await api.addReview(productId, rating, reviewText, token);
      const updated = await api.getReviews(productId);
      setReviews(Array.isArray(updated) ? updated : []);
      setReviewText('');
      setRating(5);
      alert(t('submit'));
    } catch (err: any) {
      alert(err.message || t('submit'));
    }
  };

  if (loading) return <p>{t('loading')}</p>;
  if (error) return <p className="error">{error}</p>;
  if (!product) return <p>{t('product_not_found')}</p>;

  return (
    <div className="product-detail-page">
      <section className="product-hero">
        <img src={product.imageUrl} alt={product.productName} />
        <div className="product-info">
          <h1>🛍️ {product.productName}</h1>
          <p className="brand">🏷️ {product.brand}</p>
          <p className="price">💰 ₺{product.price}</p>
          <p>📝 {product.description}</p>
          <div className="actions">
            <button onClick={handleAddToCart}>🛒 {t('add_to_cart')}</button>
            <button onClick={handleFavorite}>
              {isFavorite ? `❤️ ${t('favorite_removed')}` : `🤍 ${t('favorite')}`}
            </button>
            {favoriteError && <small className="error">⚠️ {favoriteError}</small>}
          </div>
        </div>
      </section>

      <section className="specs">
        <h3>🔧 {t('specs_title')}</h3>
        <div className="spec-grid">
          {specs.map((spec) => (
            <div key={spec.specID} className="spec-item">
              <span className="spec-name">📌 {spec.specName}</span>
              <span className="spec-value">➜ {spec.specValue}</span>
            </div>
          ))}
          {!specs.length && <p>-</p>}
        </div>
      </section>

      <section className="reviews">
        <div className="reviews-header">
          <h3>💬 {t('reviews_title')}</h3>
          {reviews.length > 0 && <span>({reviews.length})</span>}
        </div>
        <div className="review-list">
          {(Array.isArray(reviews) ? reviews : []).map((review) => (
            <div key={review.productReviewID} className="review-card">
              <div className="review-top">
                <strong>👤 {review.userName}</strong>
                <span>{'⭐'.repeat(review.rating)}</span>
              </div>
              <p>💭 {review.reviewText}</p>
              <small>📅 {new Date(review.reviewDate).toLocaleDateString()}</small>
            </div>
          ))}
          {!reviews.length && <p>✨ {t('reviews_none')}</p>}
        </div>

        <form className="review-form" onSubmit={submitReview}>
          <h4>📝 {t('review_form_title')}</h4>
          <label>
            ⭐ {t('rating_label')}
            <input
              type="number"
              min={1}
              max={5}
              value={rating}
              onChange={(e) => setRating(Number(e.target.value))}
            />
          </label>
          <label>
            💬 {t('comment_label')}
            <textarea
              value={reviewText}
              onChange={(e) => setReviewText(e.target.value)}
              placeholder={t('comment_label')}
            />
          </label>
          <button type="submit">✅ {t('submit')}</button>
        </form>
      </section>

      <section className="related">
        <h3>🔗 {t('related_title')}</h3>
        <div className="related-grid">
          {related.map((item) => (
            <Link to={`/products/${item.productID}`} key={item.productID} className="related-card">
              <img src={item.imageUrl} alt={item.productName} />
              <div>
                <p className="name">🛍️ {item.productName}</p>
                <p className="price">💰 ₺{item.price}</p>
              </div>
            </Link>
          ))}
          {!related.length && <p>-</p>}
        </div>
      </section>
    </div>
  );
};

export default ProductDetail;
