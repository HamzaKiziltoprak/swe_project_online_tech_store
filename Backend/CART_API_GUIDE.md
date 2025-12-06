# Cart API Guide

This guide documents all cart-related endpoints for the Online Tech Store backend API.

## Base URL
```
https://localhost:7100/api/cart
```

## Authentication
All endpoints require **Bearer token authentication** (JWT).

Include the token in the Authorization header:
```
Authorization: Bearer {your_jwt_token}
```

---

## Endpoints

### 1. Get User's Cart

Retrieves all items in the user's shopping cart.

**Endpoint:** `GET /api/cart`

**Authentication:** Required (User)

**Request:**
```http
GET https://localhost:7100/api/cart HTTP/1.1
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Cart retrieved successfully",
  "data": {
    "items": [
      {
        "cartItemID": 1,
        "productID": 5,
        "productName": "AMD Ryzen 5 5600X",
        "price": 299.99,
        "count": 2,
        "productImageUrl": "https://example.com/images/ryzen5.jpg",
        "subtotal": 599.98
      },
      {
        "cartItemID": 2,
        "productID": 8,
        "productName": "RTX 3070 Ti",
        "price": 799.99,
        "count": 1,
        "productImageUrl": "https://example.com/images/rtx3070ti.jpg",
        "subtotal": 799.99
      }
    ],
    "totalItems": 3,
    "totalPrice": 1399.97
  }
}
```

---

### 2. Add Item to Cart

Adds a product to the cart or increases quantity if already exists.

**Endpoint:** `POST /api/cart/add`

**Authentication:** Required (User)

**Request:**
```http
POST https://localhost:7100/api/cart/add HTTP/1.1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "productID": 5,
  "count": 2
}
```

**Request Parameters:**
- `productID` (int, required): ID of the product
- `count` (int, required): Quantity to add (minimum: 1)

**Response (201 Created):**
```json
{
  "cartItemID": 1,
  "productID": 5,
  "productName": "AMD Ryzen 5 5600X",
  "price": 299.99,
  "count": 2,
  "productImageUrl": "https://example.com/images/ryzen5.jpg",
  "subtotal": 599.98
}
```

**Error Responses:**

**400 Bad Request - Invalid quantity:**
```json
{
  "success": false,
  "message": "Quantity must be greater than 0",
  "data": null
}
```

**400 Bad Request - Not enough stock:**
```json
{
  "success": false,
  "message": "Not enough stock. Available: 5",
  "data": null
}
```

**404 Not Found - Product doesn't exist:**
```json
{
  "success": false,
  "message": "Product not found",
  "data": null
}
```

---

### 3. Update Cart Item Quantity

Updates the quantity of an item in the cart.

**Endpoint:** `PATCH /api/cart/{id}`

**Authentication:** Required (User)

**Request:**
```http
PATCH https://localhost:7100/api/cart/1 HTTP/1.1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "count": 3
}
```

**Path Parameters:**
- `id` (int): Cart item ID

**Request Body:**
- `count` (int, required): New quantity (minimum: 1)

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Cart item updated successfully",
  "data": {
    "cartItemID": 1,
    "productID": 5,
    "productName": "AMD Ryzen 5 5600X",
    "price": 299.99,
    "count": 3,
    "productImageUrl": "https://example.com/images/ryzen5.jpg",
    "subtotal": 899.97
  }
}
```

**Error Responses:**

**400 Bad Request - Invalid quantity:**
```json
{
  "success": false,
  "message": "Quantity must be greater than 0",
  "data": null
}
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Cart item not found",
  "data": null
}
```

---

### 4. Remove Item from Cart

Removes a single item from the cart.

**Endpoint:** `DELETE /api/cart/{id}`

**Authentication:** Required (User)

**Request:**
```http
DELETE https://localhost:7100/api/cart/1 HTTP/1.1
Authorization: Bearer {jwt_token}
```

**Path Parameters:**
- `id` (int): Cart item ID

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Item removed from cart",
  "data": {}
}
```

**Error Responses:**

**404 Not Found:**
```json
{
  "success": false,
  "message": "Cart item not found",
  "data": null
}
```

---

### 5. Clear Entire Cart

Removes all items from the user's cart.

**Endpoint:** `DELETE /api/cart`

**Authentication:** Required (User)

**Request:**
```http
DELETE https://localhost:7100/api/cart HTTP/1.1
Authorization: Bearer {jwt_token}
```

**Response (200 OK) - Cart has items:**
```json
{
  "success": true,
  "message": "Cart cleared successfully",
  "data": {
    "itemsRemoved": 3
  }
}
```

**Response (200 OK) - Cart is empty:**
```json
{
  "success": true,
  "message": "Cart is already empty",
  "data": {}
}
```

---

## Error Codes

| Code | Message | Cause |
|------|---------|-------|
| 400 | Quantity must be greater than 0 | Invalid count parameter |
| 400 | Not enough stock | Product stock is insufficient |
| 400 | Invalid request data | Missing/invalid required fields |
| 401 | Unauthorized | Missing or invalid JWT token |
| 404 | Product not found | Product ID doesn't exist |
| 404 | Cart item not found | Cart item doesn't belong to user |
| 500 | Internal server error | Unexpected error on server |

---

## Usage Examples

### JavaScript/Fetch

**Get Cart:**
```javascript
const response = await fetch('https://localhost:7100/api/cart', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  }
});
const data = await response.json();
console.log(data.data.items);
```

**Add to Cart:**
```javascript
const response = await fetch('https://localhost:7100/api/cart/add', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    productID: 5,
    count: 2
  })
});
const cartItem = await response.json();
console.log(cartItem);
```

**Update Quantity:**
```javascript
const response = await fetch('https://localhost:7100/api/cart/1', {
  method: 'PATCH',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    count: 3
  })
});
const updated = await response.json();
console.log(updated.data);
```

**Remove from Cart:**
```javascript
const response = await fetch('https://localhost:7100/api/cart/1', {
  method: 'DELETE',
  headers: {
    'Authorization': `Bearer ${jwtToken}`
  }
});
const result = await response.json();
console.log(result.message);
```

**Clear Cart:**
```javascript
const response = await fetch('https://localhost:7100/api/cart', {
  method: 'DELETE',
  headers: {
    'Authorization': `Bearer ${jwtToken}`
  }
});
const result = await response.json();
console.log(`Removed ${result.data.itemsRemoved} items`);
```

---

## Notes

- All prices and monetary values are in USD
- Cart is user-specific and identified by JWT token
- When adding an item that already exists, quantity is added to existing item
- Stock validation prevents overselling
- Cart persists across sessions (stored in database)
- Empty cart queries return empty array with totals of 0
