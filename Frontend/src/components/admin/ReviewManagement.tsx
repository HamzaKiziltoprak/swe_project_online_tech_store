import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../../lib/api';
import type { Review } from '../../lib/api';
import { showSuccess, showError } from '../../utils/toast';

interface ReviewManagementProps {
    token: string | null;
}

const ReviewManagement: React.FC<ReviewManagementProps> = ({ token }) => {
    const { t } = useTranslation();
    const [pendingReviews, setPendingReviews] = useState<Review[]>([]);
    const [loading, setLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);

    const fetchPendingReviews = async () => {
        if (!token) return;
        setLoading(true);
        setError(null);
        try {
            const reviews = await api.getPendingReviews(token);
            setPendingReviews(reviews);
        } catch (err: any) {
            setError(err.message || t('error_loading_reviews'));
        } finally {
            setLoading(false);
        }
    };

    const handleApproveReview = async (productId: number, reviewId: number) => {
        if (!token) return;
        try {
            await api.approveReview(productId, reviewId, token);
            showSuccess(t('review_approved'));
            fetchPendingReviews();
        } catch (err: any) {
            showError(err.message || 'Failed to approve review');
        }
    };

    const handleRejectReview = async (productId: number, reviewId: number) => {
        if (!token || !window.confirm(t('confirm_delete'))) return;
        try {
            await api.rejectReview(productId, reviewId, token);
            showSuccess(t('review_rejected'));
            fetchPendingReviews();
        } catch (err: any) {
            showError(err.message || 'Failed to reject review');
        }
    };

    useEffect(() => {
        fetchPendingReviews();
    }, [token]);

    return (
        <section className="review-management panel">
            <h3>💬 {t('review_management')}</h3>
            {error && <p className="error">⚠️ {error}</p>}
            {loading && <p>⏳ {t('loading')}</p>}
            {!loading && pendingReviews.length > 0 && (
                <div className="reviews-table">
                    <table>
                        <thead>
                            <tr>
                                <th>🛍️ {t('product')}</th>
                                <th>👤 {t('reviewer')}</th>
                                <th>⭐ {t('rating')}</th>
                                <th>💬 {t('comment')}</th>
                                <th>📅 {t('date')}</th>
                                <th>⚙️ {t('actions')}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {pendingReviews.map((review) => (
                                <tr key={review.productReviewID}>
                                    <td>{review.productName || `Product #${review.productID}`}</td>
                                    <td>{review.userName}</td>
                                    <td>
                                        <span className="rating-stars">
                                            {'⭐'.repeat(review.rating)}
                                        </span>
                                    </td>
                                    <td className="review-text-cell">{review.reviewText}</td>
                                    <td>{new Date(review.reviewDate).toLocaleDateString()}</td>
                                    <td>
                                        <div className="review-actions">
                                            <button
                                                className="approve-btn"
                                                onClick={() => handleApproveReview(review.productID!, review.productReviewID)}
                                            >
                                                ✅ {t('approve')}
                                            </button>
                                            <button
                                                className="reject-btn"
                                                onClick={() => handleRejectReview(review.productID!, review.productReviewID)}
                                            >
                                                ❌ {t('reject')}
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
            {!loading && pendingReviews.length === 0 && (
                <p className="empty-message">✨ {t('no_pending_reviews')}</p>
            )}
        </section>
    );
};

export default ReviewManagement;
