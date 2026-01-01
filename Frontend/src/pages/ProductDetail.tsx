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
import { showSuccess, showError, showInfo } from '../utils/toast';
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
  const [isAddingToCart, setIsAddingToCart] = useState(false);

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
        showError(err.message || 'Failed to load product');
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
      } catch (err: any) {
        console.error('Failed to check favorite status:', err);
      }
    };
    checkFavorite();
  }, [productId, token]);

  // ✅ Sepete Ekle - Toast notification ile
  const handleAddToCart = async () => {
    if (!token) {
      showInfo('Please login to add items to cart');
      navigate('/login');
      return;
    }

    setIsAddingToCart(true);
    try {
      await api.addToCart(productId, 1, token);
      showSuccess(`${product?.productName} added to cart!`);
    } catch (err: any) {
      showError(err.message || 'Failed to add to cart');
    } finally {
      setIsAddingToCart(false);
    }
  };

  // ✅ FIX: Favoriler - Optimistic UI + Toast notification
  const handleFavorite = async () => {
    if (!token) {
      showInfo('Please login to manage favorites');
      navigate('/login');
      return;
    }

    // Optimistic UI update
    const previousState = isFavorite;
    setIsFavorite(!isFavorite);

    try {
      await api.toggleFavorite(productId, token);

      // Toast notification with correct message
      if (!previousState) {
        showSuccess(`${product?.productName} added to favorites!`);
      } else {
        showInfo(`${product?.productName} removed from favorites`);
      }
    } catch (err: any) {
      // Revert on error
      setIsFavorite(previousState);
      showError(err.message || 'Failed to update favorites');
    }
  };

  // ✅ Yorum Gönder - Toast notification ile
  const submitReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) {
      showInfo('Please login to submit a review');
      navigate('/login');
      return;
    }

    // Validation
    if (reviewText.trim().length < 10) {
      showError('Review must be at least 10 characters long');
      return;
    }

    try {
      await api.addReview(productId, rating, reviewText, token);
      const updated = await api.getReviews(productId);
      setReviews(Array.isArray(updated) ? updated : []);
      setReviewText('');
      setRating(5);
      showSuccess('Review submitted successfully!');
    } catch (err: any) {
      showError(err.message || 'Failed to submit review');
    }
  };

  if (loading) {
    return (
      <div className="loading-container">
        <div className="loading-spinner"></div>
        <p>{t('loading')}</p>
      </div>
    );
  }

  if (error) return <p className="error-message">{error}</p>;
  if (!product) return <p className="error-message">{t('product_not_found')}</p>;

  return (
    <div className="product-detail-page">
      {/* Hero Section */}
      <section className="product-hero">
        <div className="product-image-container">
          <img src={product.imageUrl} alt={product.productName} className="product-image" />
        </div>

        <div className="product-info">
          <div className="product-header">
            <span className="product-brand">{product.brand}</span>
            <h1 className="product-title">{product.productName}</h1>
          </div>

          <div className="product-price-section">
            <span className="price-label">Price</span>
            <span className="product-price">
              ₺{product.price.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
            </span>
          </div>

          <p className="product-description">{product.description}</p>

          {/* Action Buttons */}
          <div className="product-actions">
            <button
              className="btn-add-to-cart-hero"
              onClick={handleAddToCart}
              disabled={isAddingToCart}
            >
              <svg className="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"
                />
              </svg>
              <span>{isAddingToCart ? 'Adding...' : t('add_to_cart')}</span>
            </button>

            {/* ✅ FIX: Favoriler butonu - Doğru metin ve ikon */}
            <button className="btn-favorite" onClick={handleFavorite} aria-label="Toggle favorite">
              <svg
                className="icon"
                viewBox="0 0 24 24"
                fill={isFavorite ? 'currentColor' : 'none'}
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
                />
              </svg>
              <span>
                {isFavorite
                  ? t('favorite_remove') || 'Remove from Favorites'
                  : t('favorite_add') || 'Add to Favorites'}
              </span>
            </button>
          </div>
        </div>
      </section>

      {/* Sticky CTA */}
      <div className="sticky-cta">
        <div className="sticky-cta-content">
          <div className="sticky-product-info">
            <span className="sticky-product-name">{product.productName}</span>
            <span className="sticky-product-price">
              ₺{product.price.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
            </span>
          </div>
          <button
            className="btn-add-to-cart"
            onClick={handleAddToCart}
            disabled={isAddingToCart}
          >
            <svg className="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"
              />
            </svg>
            <span>{isAddingToCart ? 'Adding...' : t('add_to_cart')}</span>
          </button>
        </div>
      </div>

      {/* Specifications */}
      <section className="product-section specs-section">
        <h2 className="section-title">{t('specs_title') || 'Technical Specifications'}</h2>
        <div className="spec-grid">
          {specs.map((spec) => (
            <div key={spec.specID} className="spec-item">
              <span className="spec-name">{spec.specName}</span>
              <span className="spec-value">{spec.specValue}</span>
            </div>
          ))}
          {!specs.length && <p className="empty-message">No specifications available</p>}
        </div>
      </section>

      {/* Reviews */}
      <section className="product-section reviews-section">
        <div className="reviews-header">
          <h2 className="section-title">{t('reviews_title') || 'Customer Reviews'}</h2>
          {reviews.length > 0 && (
            <span className="review-count">
              {reviews.length} {reviews.length === 1 ? 'Review' : 'Reviews'}
            </span>
          )}
        </div>

        <div className="review-list">
          {(Array.isArray(reviews) ? reviews : []).map((review) => (
            <div key={review.productReviewID} className="review-card">
              <div className="review-header">
                <div className="review-user">
                  <div className="user-avatar">{review.userName.charAt(0).toUpperCase()}</div>
                  <strong className="user-name">{review.userName}</strong>
                </div>
                <div className="review-rating">
                  {[...Array(5)].map((_, i) => (
                    <svg
                      key={i}
                      className={`star ${i < review.rating ? 'filled' : ''}`}
                      viewBox="0 0 24 24"
                      fill={i < review.rating ? 'currentColor' : 'none'}
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"
                      />
                    </svg>
                  ))}
                </div>
              </div>
              <p className="review-text">{review.reviewText}</p>
              <small className="review-date">
                {new Date(review.reviewDate).toLocaleDateString('tr-TR', {
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                })}
              </small>
            </div>
          ))}
          {!reviews.length && (
            <p className="empty-message">No reviews yet. Be the first to review this product!</p>
          )}
        </div>

        {/* Review Form */}
        <form className="review-form" onSubmit={submitReview}>
          <h3 className="form-title">{t('review_form_title') || 'Write a Review'}</h3>

          <div className="form-group">
            <label className="form-label">{t('rating_label') || 'Rating'}</label>
            <div className="rating-input">
              {[1, 2, 3, 4, 5].map((star) => (
                <button
                  key={star}
                  type="button"
                  className={`star-button ${rating >= star ? 'active' : ''}`}
                  onClick={() => setRating(star)}
                >
                  <svg
                    className="star"
                    viewBox="0 0 24 24"
                    fill={rating >= star ? 'currentColor' : 'none'}
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"
                    />
                  </svg>
                </button>
              ))}
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">{t('comment_label') || 'Your Review'}</label>
            <textarea
              className="form-textarea"
              value={reviewText}
              onChange={(e) => setReviewText(e.target.value)}
              placeholder={t('comment_placeholder') || 'Share your experience with this product...'}
              rows={4}
              minLength={10}
              required
            />
            <small className="form-hint">{reviewText.length}/1000 characters (minimum 10)</small>
          </div>

          <button type="submit" className="btn-submit-review">
            {t('submit') || 'Submit Review'}
          </button>
        </form>
      </section>

      {/* Related Products */}
      <section className="product-section related-section">
        <h2 className="section-title">{t('related_title') || 'You May Also Like'}</h2>
        <div className="related-grid">
          {related.map((item) => (
            <Link to={`/products/${item.productID}`} key={item.productID} className="related-card">
              <div className="related-image-container">
                <img src={item.imageUrl} alt={item.productName} />
              </div>
              <div className="related-info">
                <p className="related-name">{item.productName}</p>
                <p className="related-price">
                  ₺{item.price.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                </p>
              </div>
            </Link>
          ))}
          {!related.length && <p className="empty-message">No related products found</p>}
        </div>
      </section>
    </div>
  );
};

export default ProductDetail;
