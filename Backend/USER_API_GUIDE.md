# User & Identity API Endpoints

## 🎯 Tamamlanan Özellikler

### ✅ Identity Yapılandırması
- Password Policy: Min 8 karakter, büyük/küçük harf, rakam, özel karakter
- Lockout Protection: 5 başarısız denemeden sonra 15 dakika kilitleme
- Unique Email zorunluluğu

### ✅ JWT Authentication
- Bearer token ile authentication
- Swagger'da JWT test desteği

### ✅ Endpoint'ler

---

## 📌 Public Endpoints (Authentication Gerektirmez)

### 1. Register (Custom)
```http
POST /api/accounts/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully! Please use /login endpoint to get your access token.",
  "data": {
    "id": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "address": null,
    "createdAt": "2025-12-04T10:30:00Z",
    "roles": ["Customer"]
  }
}
```

---

### 2. Login (Identity API)
```http
POST /login?useCookies=false
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response:**
```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "..."
}
```

**Not:** `useCookies=false` parametresi önemli! Token response'da dönmesini sağlar.

---

### 3. Refresh Token (Identity API)
```http
POST /refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token-here"
}
```

---

## 🔒 Protected Endpoints (Bearer Token Gerektirir)

### Authorization Header Formatı:
```
Authorization: Bearer {your-access-token}
```

---

### 4. Get Profile
```http
GET /api/accounts/profile
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Profile retrieved successfully",
  "data": {
    "id": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "address": "123 Main St",
    "createdAt": "2025-12-04T10:30:00Z",
    "roles": ["Customer"]
  }
}
```

---

### 5. Update Profile
```http
PUT /api/accounts/profile
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Smith",
  "address": "456 New Street"
}
```

---

### 6. Change Password
```http
POST /api/accounts/change-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

---

## 👑 Admin Only Endpoints

### 7. Get All Users
```http
GET /api/accounts/users
Authorization: Bearer {admin-token}
```

### 8. Get User by ID
```http
GET /api/accounts/users/{id}
Authorization: Bearer {admin-token}
```

### 9. Assign Role to User
```http
POST /api/accounts/assign-role
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "userId": 5,
  "roleName": "Admin"
}
```

### 10. Remove Role from User
```http
DELETE /api/accounts/remove-role
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "userId": 5,
  "roleName": "Admin"
}
```

### 11. Get All Roles
```http
GET /api/accounts/roles
Authorization: Bearer {admin-token}
```

**Response:**
```json
{
  "success": true,
  "message": "Retrieved 2 roles",
  "data": ["Admin", "Customer"]
}
```

---

## 🧪 Test Adımları

### 1. Projeyi Çalıştır
```powershell
cd "c:\Users\hamza\OneDrive\Belgeler\GitHub\OnlineTechStore\Backend\Backend"
dotnet run --launch-profile "https"
```

### 2. Swagger'ı Aç
```
https://localhost:7100/swagger
```

### 3. Test Senaryosu

#### A. Yeni Kullanıcı Kaydı
1. `POST /api/accounts/register` ile kayıt ol
2. Password gereksinimleri:
   - Min 8 karakter
   - En az 1 büyük harf
   - En az 1 küçük harf
   - En az 1 rakam
   - En az 1 özel karakter (!@#$%^&*)

#### B. Login ve Token Al
1. `POST /login?useCookies=false` ile giriş yap
2. Response'dan `accessToken` kopyala

#### C. Swagger'da Token Kullan
1. Swagger sağ üstteki **🔓 Authorize** butonuna tıkla
2. Token'ı yapıştır (sadece token'ı, "Bearer" yazmadan)
3. **Authorize** butonuna tıkla
4. Artık protected endpoint'leri test edebilirsin

#### D. Profile İşlemleri
1. `GET /api/accounts/profile` ile kendi bilgilerini gör
2. `PUT /api/accounts/profile` ile bilgilerini güncelle
3. `POST /api/accounts/change-password` ile şifre değiştir

#### E. Admin İşlemleri (Önce Admin Olmalısın)
1. Database'e bak veya DbSeeder tarafından oluşturulan admin'i kullan:
   - Email: `admin@example.com`
   - Password: `Admin@123`
2. Admin token'ı ile:
   - `GET /api/accounts/users` → Tüm kullanıcıları listele
   - `POST /api/accounts/assign-role` → Birine admin rolü ver
   - `GET /api/accounts/roles` → Rolleri listele

---

## 🔐 Default Admin Hesabı

DbSeeder tarafından otomatik oluşturulan admin:
```
Email: admin@example.com
Password: Admin@123
Roles: [Admin]
```

---

## ✨ API Response Formatı

Tüm endpoint'ler standart format kullanır:

### Başarılı Response
```json
{
  "success": true,
  "message": "Operation successful",
  "data": { /* actual data */ }
}
```

### Hata Response
```json
{
  "success": false,
  "message": "Error message",
  "errors": [
    "Detailed error 1",
    "Detailed error 2"
  ]
}
```

---

## 🚀 Sonraki Adımlar

User/Identity/Roles kısmı tamamlandı! ✅

Şimdi diğer modüllere geçebiliriz:
1. **ProductsController** (Ürün yönetimi)
2. **CategoriesController** (Kategori yönetimi)
3. **CartController** (Sepet işlemleri)
4. **OrdersController** (Sipariş yönetimi)
5. **ReviewsController** (Yorum sistemi)

---

## 📊 Güvenlik Özellikleri

✅ JWT Bearer Authentication  
✅ Role-based Authorization (Admin/Customer)  
✅ Password Policy Enforcement  
✅ Brute Force Protection (Lockout after 5 attempts)  
✅ Unique Email Requirement  
✅ CORS Protection  
✅ Input Validation  
✅ Standardized Error Responses  
