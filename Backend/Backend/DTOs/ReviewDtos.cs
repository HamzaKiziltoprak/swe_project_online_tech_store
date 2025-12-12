using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    /// <summary>
    /// Review display DTO
    /// </summary>
    public class ReviewDto
    {
        public int ProductReviewID { get; set; }
        public int ProductID { get; set; }
        public string? ProductName { get; set; }
        public string UserName { get; set; } = null!;
        public int Rating { get; set; } // 1-5
        public string? ReviewText { get; set; }
        public DateTime ReviewDate { get; set; }
        public bool IsVerifiedPurchase { get; set; }
    }

    /// <summary>
    /// DTO for creating a review
    /// </summary>
    public class CreateReviewDto
    {
        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 1000 characters")]
        public string? ReviewText { get; set; }
    }

    /// <summary>
    /// DTO for updating a review
    /// </summary>
    public class UpdateReviewDto
    {
        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 1000 characters")]
        public string? ReviewText { get; set; }
    }

    /// <summary>
    /// Product review summary with rating statistics
    /// </summary>
    public class ProductReviewSummaryDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    /// <summary>
    /// Paged review results
    /// </summary>
    public class PagedReviewResult
    {
        public List<ReviewDto> Reviews { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// Review filter parameters
    /// </summary>
    public class ReviewFilterParams
    {
        public int? Rating { get; set; } // Filter by specific rating
        public string? SortBy { get; set; } = "ReviewDate"; // ReviewDate, Rating
        public string? SortOrder { get; set; } = "desc"; // asc, desc
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
