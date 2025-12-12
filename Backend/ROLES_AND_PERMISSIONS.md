# 🔐 Role-Based Access Control (RBAC) Dokümantasyonu

## Sistem Rolleri

Sistemde **4 ana rol** tanımlanmıştır:

| Role | Türü | Açıklama |
|------|------|----------|
| **Customer** | Kullanıcı | Alışveriş yapan normal müşteri |
| **Admin** | Yönetici | Sistem yönetimi ve rol atama |
| **ProductManager** | Yönetici | Ürün yönetimi ve review moderasyonu |
| **CompanyOwner** | Yönetici | İstatistikleri görüntüleme (salt okunur) |

---

## 📋 Endpoint İzinleri Tablosu

### 1️⃣ **PRODUCTS CONTROLLER** (`/api/products`)

| Endpoint | HTTP | İzin Gerekli | İzin Veren Roller |
|----------|------|--------------|-------------------|
| `GET /` | GET | ❌ Yok | Herkes (Public) |
| `GET /{id}` | GET | ❌ Yok | Herkes (Public) |
| `GET /category/{categoryId}` | GET | ❌ Yok | Herkes (Public) |
| `GET /featured` | GET | ❌ Yok | Herkes (Public) |
| `POST /` | POST | ✅ **Admin, ProductManager** | Admin, ProductManager |
| `PUT /{id}` | PUT | ✅ **Admin, ProductManager** | Admin, ProductManager |
| `DELETE /{id}` | DELETE | ✅ **Admin** | Admin |
| `GET /related/{productId}` | GET | ❌ Yok | Herkes (Public) |
| `POST /compare` | POST | ❌ Yok | Herkes (Public) |
| `GET /similar/{productId}` | GET | ❌ Yok | Herkes (Public) |
| `GET /comparison/details` | GET | ❌ Yok | Herkes (Public) |
| `GET /low-stock` | GET | ✅ **Admin, ProductManager** | Admin, ProductManager |
| `PUT /critical-stock/{id}` | PUT | ✅ **Admin, ProductManager** | Admin, ProductManager |
| `GET /brands` | GET | ❌ Yok | Herkes (Public) |

**Sonuç**: Ürün **okuma işlemleri herkese açık**, **yazma/güncelleme işlemleri Admin ve ProductManager tarafından yapılabilir**

---

### 2️⃣ **ORDERS CONTROLLER** (`/api/orders`)

| Endpoint | HTTP | İzin Gerekli | İzin Veren Roller |
|----------|------|--------------|-------------------|
| `POST /` | POST | ✅ **Customer** | Sadece Customer |
| `GET /my-orders` | GET | ✅ **Authorize** | Giriş yapan herkes |
| `GET /my-orders/{id}` | GET | ✅ **Authorize** | Giriş yapan herkes |
| `POST /one-click-buy` | POST | ✅ **Customer** | Sadece Customer |
| `PUT /{id}/status` | PUT | ✅ **Admin** | Admin |
| `DELETE /{id}` | DELETE | ✅ **Admin** | Admin |

**Sonuç**: 
- **Sipariş oluşturma**: Sadece Customer (Müşteri hesapları)
- **Sipariş görüntüleme**: Kendi siparişlerini görebilirler
- **Sipariş yönetimi**: Sadece Admin

---

### 3️⃣ **REVIEWS CONTROLLER** (`/api/reviews`)

| Endpoint | HTTP | İzin Gerekli | İzin Veren Roller |
|----------|------|--------------|-------------------|
| `GET /{productId}` | GET | ❌ Yok | Herkes (Public) |
| `POST /` | POST | ✅ **Customer** | Sadece Customer |
| `PUT /{id}` | PUT | ✅ **Customer** | Sadece Customer |
| `DELETE /{id}` | DELETE | ✅ **Customer** | Sadece Customer |
| `GET /my-reviews` | GET | ✅ **Customer** | Sadece Customer |
| `GET /pending` | GET | ✅ **Admin, ProductManager** | Admin, ProductManager |
| `PUT /{id}/approve` | PUT | ✅ **Admin, ProductManager** | Admin, ProductManager |
| `PUT /{id}/reject` | PUT | ✅ **Admin, ProductManager** | Admin, ProductManager |

**Sonuç**: 
- **Review yazma**: Sadece Customer (Müşteriler)
- **Review moderasyonu**: Admin, ProductManager

---

### 4️⃣ **TRANSACTIONS CONTROLLER** (`/api/transactions`)

| Endpoint | HTTP | İzin Gerekli | İzin Veren Roller |
|----------|------|--------------|-------------------|
| `GET /my-transactions` | GET | ✅ **Authorize** | Giriş yapan herkes |
| `GET /` | GET | ✅ **Admin, ProductManager** | Admin, ProductManager |
| `GET /{id}` | GET | ✅ **Authorize** | Giriş yapan herkes |
| `GET /financial-summary` | GET | ✅ **Admin, CompanyOwner** | Admin, CompanyOwner |
| `POST /refund` | POST | ✅ **Admin** | Admin |

**Sonuç**: 
- **Kişisel işlemleri görüntüleme**: Tüm kullanıcılar
- **Tüm işlemleri görüntüleme**: Admin, ProductManager
- **Mali rapor**: Admin, CompanyOwner (Read-Only)

---

### 5️⃣ **CART CONTROLLER** (`/api/cart`)

| Endpoint | HTTP | İzin Gerekli | İzin Veren Roller |
|----------|------|--------------|-------------------|
| `GET /` | GET | ✅ **Customer** | Sadece Customer |
| `POST /items` | POST | ✅ **Customer** | Sadece Customer |
| `DELETE /items/{id}` | DELETE | ✅ **Customer** | Sadece Customer |

**Sonuç**: Sepet işlemleri sadece Customer (Müşteri) hesaplara açık

---

### 6️⃣ **ANALYTICS CONTROLLER** (`/api/analytics`)

| Endpoint | HTTP | İzin Gerekli | İzin Veren Roller |
|----------|------|--------------|-------------------|
| Tüm endpoints | GET | ✅ **Admin, ProductManager, CompanyOwner** | Admin, ProductManager, CompanyOwner |

**Alt Endpoints**:
- `GET /dashboard` - Dashboard özetleri
- `GET /top-products` - En çok satan ürünler
- `GET /product-count-by-category` - Kategori başına ürün sayısı
- `GET /average-order-value` - Ortalama sipariş değeri
- `GET /top-customers` - En iyi müşteriler
- `GET /return-rate` - İade oranı
- `GET /daily-revenue` - Günlük gelir

**Sonuç**: **Salt okunur** istatistik dashboardu - Admin, ProductManager ve CompanyOwner

---

### 7️⃣ **ADMIN CONTROLLER** (`/api/admin`)

| Endpoint | HTTP | İzin Gerekli | İzin Veren Roller |
|----------|------|--------------|-------------------|
| Tüm endpoints | - | ✅ **Admin** | Admin |

**Alt Endpoints**:
- `POST /assign-role` - Rol atama
- `GET /all-users` - Tüm kullanıcıları listele
- `POST /change-theme` - Tema değiştirme

**Geçerli Roller**: Admin, ProductManager, Customer, CompanyOwner

**Sonuç**: **Sistem yönetimi** sadece Admin

---

### 8️⃣ **ACCOUNTS CONTROLLER** (`/api/accounts`)

| Endpoint | HTTP | İzin Gerekli | İzin Veren Roller |
|----------|------|--------------|-------------------|
| `POST /register` | POST | ❌ Yok | Herkes (Public) |
| `POST /login` | POST | ❌ Yok | Herkes (Public) |
| `GET /profile` | GET | ✅ **Authorize** | Giriş yapan herkes |
| `PUT /profile` | PUT | ✅ **Authorize** | Giriş yapan herkes |
| `POST /change-password` | POST | ✅ **Authorize** | Giriş yapan herkes |

**Sonuç**: Kayıt ve giriş açık, profil güncellemesi giriş gerekli

---

## 👥 Rol Tabanlı Erişim Özeti

### 📌 **CUSTOMER** (Müşteri)
```
Açık Erişim:
✅ Ürünleri görüntüleme, arama, filtreleme
✅ Ürünleri karşılaştırma

Customer Hesabı Gerekli:
✅ Sepet yönetimi
✅ Favorilere ekleme
✅ Review yazma/düzenleme/silme
✅ Satın alma (normal checkout)
✅ One-Click Buy
✅ Siparişlerini görüntüleme
✅ İade talebinde bulunma
✅ Profil güncelleme

Yasak:
❌ Ürün yönetimi
❌ Rol atama
❌ Analytics dashboard
❌ Review moderasyonu

⚠️ NOT: Admin/ProductManager/CompanyOwner hesapları alışveriş yapamaz.
Alışveriş için ayrı Customer hesabı oluşturulmalı.
```

---

### 👨‍💼 **PRODUCTMANAGER** (Ürün Yöneticisi)
```
İş Rolü - Alışveriş Yapamaz

Operasyonel Yetkiler:
✅ Yeni ürün ekleme
✅ Ürünü güncelleme
✅ Stok seviyesi güncelleme
✅ Düşük stoklu ürünleri görüntüleme
✅ Review moderasyonu (Onaylama/Reddetme)
✅ Pending reviews görüntüleme

Raporlama & Analitik:
✅ Analytics Dashboard
✅ Tüm işlemleri görüntüleme (Transactions)
✅ Satış raporları
✅ Stok raporları

Yasak:
❌ Sepet/Favori/Sipariş (İş hesabı)
❌ Kullanıcı rol atama
❌ Sistem teması değiştirme
❌ Ürün silme (sadece Admin)
```

---

### 👔 **COMPANYOWNER** (Şirket Sahibi)
```
İş Rolü - Alışveriş Yapamaz

Yetkiler (SALT OKUNUR):
✅ Analytics Dashboard:
   - Toplam gelir
   - Sipariş sayısı
   - En çok satan ürünler
   - Ürün sayısı kategoriye göre
   - Ortalama sipariş değeri
   - En iyi müşteriler
   - İade oranı
   - Günlük gelir trendi

✅ Mali özet görüntüleme (Financial Summary)
✅ İşlem raporları görüntüleme

Yasak:
❌ Sepet/Favori/Sipariş (İş hesabı)
❌ Ürün yönetimi
❌ Rol atama
❌ Veri yazma/güncelleme (Salt okuma)
```

---

### 🔑 **ADMIN** (Sistem Yöneticisi)
```
TÜM İZİNLER ✅

Ana Görevler:
✅ Tüm PRODUCT MANAGER izinleri
✅ Tüm COMPANY OWNER izinleri
✅ Kullanıcı rol atama
✅ Sistem teması değiştirme
✅ Sipariş durumu güncelleme
✅ Sipariş silme
✅ Para iadesi işlemi
✅ Tüm işlemleri görüntüleme

Root Access:
✅ Tüm veri okuma/yazma
✅ Sistem yapılandırması
```

---

## 🔄 İzin Kontrol Mekanizmaları

### 1. **Authorize Attribute** (Controller Level)
```csharp
[Authorize]                                      // Giriş gerekli
[Authorize(Roles = "Admin")]                     // Sadece Admin
[Authorize(Roles = "Admin,ProductManager")]      // Admin veya ProductManager
```

### 2. **Runtime Role Check** (Action Level)
```csharp
if (User.IsInRole("Admin")) { ... }
```

### 3. **JWT Token**
- Login sırasında JWT token üretilir
- Token içinde user roles bilgisi yer alır
- Her request'te Authorization header'ında gönderilir

---

## 📊 Erişim Matrisi

| Feature | Customer | ProductManager | CompanyOwner | Admin |
|---------|----------|-----------------|--------------|-------|
| **Ürün Okuma** | ✅ | ✅ | ✅ | ✅ |
| **Ürün Yazma** | ❌ | ✅ | ❌ | ✅ |
| **Sepet Yönetimi** | ✅ | ❌ | ❌ | ❌ |
| **Favoriler** | ✅ | ❌ | ❌ | ❌ |
| **Sipariş Oluşturma** | ✅ | ❌ | ❌ | ❌ |
| **One-Click Buy** | ✅ | ❌ | ❌ | ❌ |
| **İade Talebi** | ✅ | ❌ | ❌ | ❌ |
| **Sipariş Yönetimi** | ❌ | ❌ | ❌ | ✅ |
| **Review Yazma** | ✅ | ❌ | ❌ | ❌ |
| **Review Moderasyonu** | ❌ | ✅ | ❌ | ✅ |
| **Analytics** | ❌ | ✅ | ✅ (RO) | ✅ |
| **Rol Atama** | ❌ | ❌ | ❌ | ✅ |
| **Sistem Ayarları** | ❌ | ❌ | ❌ | ✅ |
| **İşlem Görüntüleme** | ✅ (Kendisi) | ✅ (Tümü) | ✅ (RO) | ✅ |

---

## 🎯 Sonuç ve Öneriler

### ✅ Mevcut Durum
- Tüm kritik endpoint'ler korumalı
- Rol tabanlı erişim kontrol tanımlanmış
- Admin tam yetkiye sahip
- CompanyOwner salt okunur analytics erişimi

### ✅ Güncel Durum

Tüm rol yetkileri profesyonel iş aktarı ile uyumlu şekilde yapılandırılmıştır:

1. ✅ **Customer (Müşteri Rolü)**
   - Sepet, favori, sipariş, review, iade işlemleri
   - Alışveriş odaklı tüm yetkiler
   - ⚠️ İş rolleri (Admin/PM/Owner) alışveriş yapamaz

2. ✅ **ProductManager (Ürün Yöneticisi)**
   - Ürün ekleme/güncelleme
   - Stok yönetimi
   - Review moderasyonu
   - Analytics dashboard erişimi (YENİ!)

3. ✅ **CompanyOwner (Şirket Sahibi)**
   - Analytics dashboard (salt okunur)
   - Mali raporlar
   - Sadece izleme yetkisi

4. ✅ **Admin (Sistem Yöneticisi)**
   - Tüm ProductManager yetkileri
   - Tüm CompanyOwner yetkileri
   - Rol atama
   - Sistem yapılandırması

### ⚠️ Önemli Not
**Rol Ayrımı**: İş rolleri (Admin/ProductManager/CompanyOwner) ile müşteri rolU (Customer) birbirinden tamamen ayrılmıştır. Çalışanlar alışveriş yapmak isterse ayrı bir Customer hesabı oluşturmalıdır.
