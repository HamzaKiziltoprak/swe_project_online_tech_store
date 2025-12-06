# Reviews API Guide

This guide documents all review-related endpoints for the Online Tech Store backend API.

## Base URL
```
https://localhost:7100/api/products/{productId}/reviews
https://localhost:7100/api/reviews
```

## Authentication
Most endpoints require **Bearer token authentication** (JWT).

Include the token in the Authorization header:
```
Authorization: Bearer {your_jwt_token}
```

---

## Endpoints

### 1. Get Product Reviews

Retrieves all reviews for a specific product with pagination and filtering.

**Endpoint:** `GET /api/products/{productId}/reviews`

**Authentication:** Not required

**Query Parameters:**
- `rating` (int, optional): Filter by rating (1-5)
- `sortBy` (string, optional): Sort by "ReviewDate" or "Rating" (default: ReviewDate)
- `sortOrder` (string, optional): "asc" or "desc" (default: desc)
- `pageNumber` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 10)

**Request:**
```http
GET https://localhost:7100/api/products/5/reviews?rating=4&pageNumber=1&pageSize=10 HTTP/1.1
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Reviews retrieved successfully",
  "data": {
    "reviews": [
      {
        "productReviewID": 1,
        "productID": 5,
        "userName": "john_doe",
        "rating": 5,
        "reviewText": "Excellent CPU, great performance and price!",
        "reviewDate": "2025-12-05T10:30:00Z",
        "isVerifiedPurchase": true
      },
      {
        "productReviewID": 2,
        "productID": 5,
        "userName": "jane_smith",
        "rating": 4,
        "reviewText": "Good performance, slightly high price",
        "reviewDate": "2025-12-04T15:45:00Z",
        "isVerifiedPurchase": true
      }
    ],
    "totalCount": 25,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3
  }
}
```

---

### 2. Get Review Summary

Gets review statistics for a product including average rating and distribution.

**Endpoint:** `GET /api/products/{productId}/reviews/summary`

**Authentication:** Not required

**Request:**
```http
GET https://localhost:7100/api/products/5/reviews/summary HTTP/1.1
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Review summary retrieved successfully",
  "data": {
    "productID": 5,
    "productName": "AMD Ryzen 5 5600X",
    "averageRating": 4.50,
    "totalReviews": 25,
    "ratingDistribution": {
      "5": 12,
      "4": 8,
      "3": 3,
      "2": 2,
      "1": 0
    },
    "reviews": [
      {
        "productReviewID": 1,
        "productID": 5,
        "userName": "john_doe",
        "rating": 5,
        "reviewText": "Excellent CPU, great performance and price!",
        "reviewDate": "2025-12-05T10:30:00Z",
        "isVerifiedPurchase": true
      }
    ]
  }
}
```

---

### 3. Create Review

Creates a new review for a product (one per user per product).

**Endpoint:** `POST /api/products/{productId}/reviews`

**Authentication:** Required (User)

**Request:**
```http
POST https://localhost:7100/api/products/5/reviews HTTP/1.1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "rating": 5,
  "reviewText": "Excellent CPU, great performance and price!"
}
```

**Request Body:**
- `rating` (int, required): Rating from 1 to 5
- `reviewText` (string, optional): Review text (10-1000 characters)

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Review created successfully",
  "data": {
    "productReviewID": 1,
    "productID": 5,
    "userName": "john_doe",
    "rating": 5,
    "reviewText": "Excellent CPU, great performance and price!",
    "reviewDate": "2025-12-05T10:30:00Z",
    "isVerifiedPurchase": true
  }
}
```

**Error Responses:**

**400 Bad Request - Already reviewed:**
```json
{
  "success": false,
  "message": "You have already reviewed this product",
  "data": null
}
```

**400 Bad Request - Invalid rating:**
```json
{
  "success": false,
  "message": "Invalid request data",
  "data": ["Rating must be between 1 and 5"]
}
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Product not found",
  "data": null
}
```

---

### 4. Update Review

Updates an existing review (user can only update their own).

**Endpoint:** `PUT /api/products/{productId}/reviews/{reviewId}`

**Authentication:** Required (User - must own the review)

**Request:**
```http
PUT https://localhost:7100/api/products/5/reviews/1 HTTP/1.1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "rating": 4,
  "reviewText": "Good CPU, great performance overall"
}
```

**Request Body:**
- `rating` (int, required): Rating from 1 to 5
- `reviewText` (string, optional): Review text (10-1000 characters)

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Review updated successfully",
  "data": {
    "productReviewID": 1,
    "productID": 5,
    "userName": "john_doe",
    "rating": 4,
    "reviewText": "Good CPU, great performance overall",
    "reviewDate": "2025-12-05T11:00:00Z",
    "isVerifiedPurchase": true
  }
}
```

**Error Responses:**

**403 Forbidden - Not owner:**
```
403 Forbidden
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Review not found",
  "data": null
}
```

---

### 5. Delete Review

Deletes a review (user can delete their own, admin can delete any).

**Endpoint:** `DELETE /api/products/{productId}/reviews/{reviewId}`

**Authentication:** Required (User/Admin)

**Request:**
```http
DELETE https://localhost:7100/api/products/5/reviews/1 HTTP/1.1
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Review deleted successfully",
  "data": {}
}
```

**Error Responses:**

**403 Forbidden - Not owner:**
```
403 Forbidden
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Review not found",
  "data": null
}
```

---

### 6. Get My Reviews

Retrieves all reviews created by the authenticated user.

**Endpoint:** `GET /api/reviews/my-reviews`

**Authentication:** Required (User)

**Query Parameters:**
- `rating` (int, optional): Filter by rating (1-5)
- `sortBy` (string, optional): Sort by "ReviewDate" or "Rating" (default: ReviewDate)
- `sortOrder` (string, optional): "asc" or "desc" (default: desc)
- `pageNumber` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 10)

**Request:**
```http
GET https://localhost:7100/api/reviews/my-reviews?pageNumber=1&pageSize=10 HTTP/1.1
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Your reviews retrieved successfully",
  "data": {
    "reviews": [
      {
        "productReviewID": 1,
        "productID": 5,
        "userName": "john_doe",
        "rating": 5,
        "reviewText": "Excellent CPU!",
        "reviewDate": "2025-12-05T10:30:00Z",
        "isVerifiedPurchase": true
      },
      {
        "productReviewID": 3,
        "productID": 8,
        "userName": "john_doe",
        "rating": 4,
        "reviewText": "Great graphics card",
        "reviewDate": "2025-12-03T14:20:00Z",
        "isVerifiedPurchase": true
      }
    ],
    "totalCount": 5,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  }
}
```

---

## Error Codes

| Code | Message | Cause |
|------|---------|-------|
| 400 | Invalid request data | Missing/invalid required fields |
| 400 | You have already reviewed this product | User already has a review for this product |
| 400 | Rating must be between 1 and 5 | Invalid rating value |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | User is not the review owner |
| 404 | Product not found | Product ID doesn't exist |
| 404 | Review not found | Review doesn't belong to product |
| 500 | Internal server error | Unexpected server error |

---

## Usage Examples

### JavaScript/Fetch

**Get Product Reviews:**
```javascript
const response = await fetch('https://localhost:7100/api/products/5/reviews?pageSize=10', {
  method: 'GET',
  headers: {
    'Content-Type': 'application/json'
  }
});
const data = await response.json();
console.log(data.data.reviews);
```

**Get Review Summary:**
```javascript
const response = await fetch('https://localhost:7100/api/products/5/reviews/summary', {
  method: 'GET',
  headers: {
    'Content-Type': 'application/json'
  }
});
const summary = await response.json();
console.log(`Average Rating: ${summary.data.averageRating} stars`);
console.log(`Total Reviews: ${summary.data.totalReviews}`);
```

**Create Review:**
```javascript
const response = await fetch('https://localhost:7100/api/products/5/reviews', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    rating: 5,
    reviewText: "Excellent product!"
  })
});
const review = await response.json();
console.log(review.data);
```

**Update Review:**
```javascript
const response = await fetch('https://localhost:7100/api/products/5/reviews/1', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    rating: 4,
    reviewText: "Updated review text"
  })
});
const updated = await response.json();
console.log(updated.data);
```

**Delete Review:**
```javascript
const response = await fetch('https://localhost:7100/api/products/5/reviews/1', {
  method: 'DELETE',
  headers: {
    'Authorization': `Bearer ${jwtToken}`
  }
});
const result = await response.json();
console.log(result.message);
```

**Get My Reviews:**
```javascript
const response = await fetch('https://localhost:7100/api/reviews/my-reviews?pageSize=10', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  }
});
const myReviews = await response.json();
console.log(myReviews.data.reviews);
```

---

## Notes

- Ratings are on a scale of 1-5
- Review text is optional but if provided, must be 10-1000 characters
- Verified Purchase flag is automatically set if user has a Delivered order for the product
- Reviews are indexed by ReviewDate (newest first) by default
- Each user can have only one review per product
- Review dates are stored in UTC timezone
