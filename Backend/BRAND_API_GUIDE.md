# 🏷️ Brand API Guide

## Overview
Brand API, ürün markalarını yönetmek için kullanılır. Bu API ile marka CRUD işlemleri ve marka bazlı sorgulama yapılabilir.

## 📋 Table of Contents
- [Base URL](#base-url)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
  - [Get All Brands](#1-get-all-brands)
  - [Get Brand by ID](#2-get-brand-by-id)
  - [Create Brand](#3-create-brand-admin-only)
  - [Update Brand](#4-update-brand-admin-only)
  - [Delete Brand](#5-delete-brand-admin-only)
  - [Get Brands with Counts](#6-get-brands-with-counts)

---

## Base URL
```
/api/brands
```

## Authentication
- Public endpoints: GET requests (read operations)
- Protected endpoints: POST, PUT, DELETE (Admin only)
- JWT Bearer Token required for protected endpoints

---

## Endpoints

### 1. Get All Brands
Retrieves all brands with optional filtering by active status.

**Endpoint:** `GET /api/brands`

**Query Parameters:**
- `isActive` (optional, boolean): Filter by active status

**Request Example:**
```http
GET /api/brands?isActive=true
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "15 brands retrieved successfully",
  "data": [
    {
      "brandID": 1,
      "brandName": "AMD",
      "description": "Advanced Micro Devices - Leading processor manufacturer",
      "logoUrl": null,
      "isActive": true,
      "productCount": 12
    },
    {
      "brandID": 2,
      "brandName": "Intel",
      "description": "World's leading semiconductor chip manufacturer",
      "logoUrl": null,
      "isActive": true,
      "productCount": 8
    }
  ]
}
```

---

### 2. Get Brand by ID
Retrieves detailed information about a specific brand including its products.

**Endpoint:** `GET /api/brands/{id}`

**Request Example:**
```http
GET /api/brands/1
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "data": {
    "brandID": 1,
    "brandName": "AMD",
    "description": "Advanced Micro Devices - Leading processor manufacturer",
    "logoUrl": null,
    "isActive": true,
    "createdAt": "2025-12-12T10:00:00Z",
    "productCount": 12,
    "products": [
      {
        "productID": 1,
        "productName": "AMD Ryzen 5 5600X",
        "brand": "AMD",
        "price": 299.99,
        "stock": 25,
        "imageUrl": "https://example.com/image.jpg",
        "categoryName": "Processors",
        "isActive": true,
        "averageRating": 4.5
      }
    ]
  }
}
```

**Error Response (404):**
```json
{
  "success": false,
  "message": "Brand not found",
  "data": null
}
```

---

### 3. Create Brand (Admin Only)
Creates a new brand.

**Endpoint:** `POST /api/brands`

**Authorization:** Bearer Token (Admin role required)

**Request Body:**
```json
{
  "brandName": "ASUS",
  "description": "Leading technology company focused on motherboards",
  "logoUrl": "https://example.com/asus-logo.png"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Brand created successfully",
  "data": {
    "brandID": 16,
    "brandName": "ASUS",
    "description": "Leading technology company focused on motherboards",
    "logoUrl": "https://example.com/asus-logo.png",
    "isActive": true,
    "productCount": 0
  }
}
```

**Error Response (400 - Duplicate):**
```json
{
  "success": false,
  "message": "A brand with this name already exists",
  "data": null
}
```

**Validation Rules:**
- `brandName`: Required, 2-100 characters
- `description`: Optional, max 500 characters
- `logoUrl`: Optional, max 200 characters

---

### 4. Update Brand (Admin Only)
Updates an existing brand.

**Endpoint:** `PUT /api/brands/{id}`

**Authorization:** Bearer Token (Admin role required)

**Request Body:**
```json
{
  "brandName": "ASUS",
  "description": "Updated description for ASUS",
  "logoUrl": "https://example.com/new-logo.png",
  "isActive": true
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Brand updated successfully",
  "data": {
    "brandID": 4,
    "brandName": "ASUS",
    "description": "Updated description for ASUS",
    "logoUrl": "https://example.com/new-logo.png",
    "isActive": true,
    "productCount": 15
  }
}
```

**Error Response (404):**
```json
{
  "success": false,
  "message": "Brand not found",
  "data": null
}
```

---

### 5. Delete Brand (Admin Only)
Soft deletes a brand (marks as inactive).

**Endpoint:** `DELETE /api/brands/{id}`

**Authorization:** Bearer Token (Admin role required)

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Brand has been marked as inactive",
  "data": "Brand deactivated successfully"
}
```

**Error Response (400 - Has Active Products):**
```json
{
  "success": false,
  "message": "Cannot delete brand with 15 active products. Deactivate products first.",
  "data": null
}
```

**Note:** You cannot delete a brand that has active products. Deactivate all products first.

---

### 6. Get Brands with Counts
Retrieves only active brands that have products, sorted by product count.

**Endpoint:** `GET /api/brands/with-counts`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Success",
  "data": [
    {
      "brandID": 1,
      "brandName": "AMD",
      "description": "Advanced Micro Devices",
      "logoUrl": null,
      "isActive": true,
      "productCount": 25
    },
    {
      "brandID": 3,
      "brandName": "NVIDIA",
      "description": "Leader in visual computing",
      "logoUrl": null,
      "isActive": true,
      "productCount": 18
    }
  ]
}
```

---

## 🔐 Authorization

### Admin Endpoints
The following endpoints require **Admin** role:
- `POST /api/brands` - Create brand
- `PUT /api/brands/{id}` - Update brand
- `DELETE /api/brands/{id}` - Delete brand

### Public Endpoints
- `GET /api/brands` - Get all brands
- `GET /api/brands/{id}` - Get brand by ID
- `GET /api/brands/with-counts` - Get brands with counts

---

## 📝 DTOs Reference

### BrandDto
```csharp
{
  "brandID": 1,
  "brandName": "AMD",
  "description": "Description",
  "logoUrl": "https://example.com/logo.png",
  "isActive": true,
  "productCount": 12
}
```

### CreateBrandDto
```csharp
{
  "brandName": "Brand Name",        // Required, 2-100 chars
  "description": "Description",     // Optional, max 500 chars
  "logoUrl": "https://..."         // Optional, max 200 chars
}
```

### UpdateBrandDto
```csharp
{
  "brandName": "Brand Name",        // Required, 2-100 chars
  "description": "Description",     // Optional, max 500 chars
  "logoUrl": "https://...",        // Optional, max 200 chars
  "isActive": true                 // Required
}
```

### BrandDetailDto
```csharp
{
  "brandID": 1,
  "brandName": "AMD",
  "description": "Description",
  "logoUrl": "https://...",
  "isActive": true,
  "createdAt": "2025-12-12T10:00:00Z",
  "productCount": 12,
  "products": [...]              // Array of ProductListDto
}
```

---

## 🎯 Use Cases

### Frontend Integration

#### 1. Display Brand Filter in Product List
```javascript
// Fetch brands for filter dropdown
const response = await fetch('/api/brands/with-counts');
const { data: brands } = await response.json();

// Display in filter
brands.forEach(brand => {
  console.log(`${brand.brandName} (${brand.productCount} products)`);
});
```

#### 2. Brand Detail Page
```javascript
// Fetch brand with products
const brandId = 1;
const response = await fetch(`/api/brands/${brandId}`);
const { data: brand } = await response.json();

// Display brand info and products
console.log(`Brand: ${brand.brandName}`);
console.log(`Products: ${brand.products.length}`);
```

#### 3. Admin: Create Brand
```javascript
// Admin creates new brand
const response = await fetch('/api/brands', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer YOUR_JWT_TOKEN',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    brandName: "New Brand",
    description: "Brand description",
    logoUrl: "https://example.com/logo.png"
  })
});

const result = await response.json();
console.log('Brand created:', result.data.brandID);
```

---

## 🚨 Error Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request (validation error, duplicate name, has active products) |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not Found |
| 500 | Internal Server Error |

---

## 📊 Database Schema

### Brands Table
```sql
CREATE TABLE "Brands" (
    "BrandID" SERIAL PRIMARY KEY,
    "BrandName" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "LogoUrl" VARCHAR(200),
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### Relationship with Products
```sql
ALTER TABLE "Products"
ADD COLUMN "BrandID" INTEGER NOT NULL,
ADD CONSTRAINT "FK_Products_Brands_BrandID" 
    FOREIGN KEY ("BrandID") 
    REFERENCES "Brands"("BrandID") 
    ON DELETE RESTRICT;
```

---

## ✅ Migration Applied
This feature requires the **AddBrandModel** migration to be applied.

### Migration Steps:
1. Brands table is created
2. Existing Product.Brand strings are migrated to Brands table
3. Products.BrandID foreign key is added
4. Old Product.Brand column is dropped

---

## 🧪 Testing

### Test Create Brand
```bash
curl -X POST http://localhost:5000/api/brands \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "brandName": "Test Brand",
    "description": "Test Description"
  }'
```

### Test Get Brands
```bash
curl -X GET http://localhost:5000/api/brands
```

### Test Get Brand by ID
```bash
curl -X GET http://localhost:5000/api/brands/1
```

---

## 📚 Related Documentation
- [Product API Guide](./PRODUCTS_API_GUIDE.md)
- [Category API Guide](./CATEGORIES_API_GUIDE.md)
- [User API Guide](./USER_API_GUIDE.md)
