# Online Tech Store - Orders API Guide

## Orders (Siparişler) Endpoints

Tüm siparişlerin yönetimi için kullanılan API endpoints.

### 1. Sepetten Sipariş Oluştur
**POST** `/api/orders`

Sepetteki ürünlerden bir sipariş oluşturur.

**Gerekli:** Authorization (JWT Token)

**Request Body:**
```json
{
  "shippingAddress": "İstanbul, Avcılar, Örnek Caddesi No:123"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "data": {
    "orderID": 1,
    "userID": 1,
    "userEmail": "user@example.com",
    "totalAmount": 299.99,
    "status": "Pending",
    "orderDate": "2025-12-06T10:30:00Z",
    "shippingAddress": "İstanbul, Avcılar, Örnek Caddesi No:123",
    "items": [
      {
        "orderItemID": 1,
        "productID": 1,
        "productName": "Wireless Mouse",
        "unitPrice": 50.00,
        "quantity": 3,
        "subtotal": 150.00
      }
    ]
  },
  "message": "Sipariş başarıyla oluşturuldu"
}
```

---

### 2. Kullanıcının Siparişlerini Getir
**GET** `/api/orders`

Giriş yapmış kullanıcının siparişlerini sayfalamalı olarak getirir.

**Gerekli:** Authorization (JWT Token)

**Query Parameters:**
- `pageNumber` (int, default: 1) - Sayfa numarası
- `pageSize` (int, default: 10) - Sayfa başına ürün sayısı
- `status` (string, optional) - Filtre: Pending, Processing, Shipped, Delivered, Cancelled

**Example:**
```
GET /api/orders?pageNumber=1&pageSize=10&status=Pending
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orders": [
      {
        "orderID": 1,
        "userID": 1,
        "userEmail": "user@example.com",
        "totalAmount": 299.99,
        "status": "Pending",
        "orderDate": "2025-12-06T10:30:00Z",
        "shippingAddress": "İstanbul, Avcılar, Örnek Caddesi No:123",
        "items": [
          {
            "orderItemID": 1,
            "productID": 1,
            "productName": "Wireless Mouse",
            "unitPrice": 50.00,
            "quantity": 3,
            "subtotal": 150.00
          }
        ]
      }
    ],
    "totalCount": 5,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  },
  "message": "Siparişleriniz getirildi"
}
```

---

### 3. Sipariş Detaylarını Getir
**GET** `/api/orders/{id}`

Belirli bir siparişin detaylarını getirir.

**Gerekli:** Authorization (JWT Token)

**URL Parameters:**
- `id` (int) - Sipariş ID'si

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderID": 1,
    "userID": 1,
    "userEmail": "user@example.com",
    "totalAmount": 299.99,
    "status": "Pending",
    "orderDate": "2025-12-06T10:30:00Z",
    "shippingAddress": "İstanbul, Avcılar, Örnek Caddesi No:123",
    "items": [
      {
        "orderItemID": 1,
        "productID": 1,
        "productName": "Wireless Mouse",
        "unitPrice": 50.00,
        "quantity": 3,
        "subtotal": 150.00
      }
    ]
  },
  "message": "Sipariş detayları getirildi"
}
```

---

### 4. Sipariş Durumunu Güncelle (Admin Only)
**PATCH** `/api/orders/{id}/status`

Bir siparişin durumunu günceller. Sadece Admin kullanıcılar yapabilir.

**Gerekli:** Authorization (JWT Token + Admin Role)

**URL Parameters:**
- `id` (int) - Sipariş ID'si

**Request Body:**
```json
{
  "status": "Processing",
  "reasonOrNotes": "Ürün depodan çıkarıldı"
}
```

**Geçerli Status Değerleri:**
- Pending
- Processing
- Shipped
- Delivered
- Cancelled

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderID": 1,
    "userID": 1,
    "userEmail": "user@example.com",
    "totalAmount": 299.99,
    "status": "Processing",
    "orderDate": "2025-12-06T10:30:00Z",
    "shippingAddress": "İstanbul, Avcılar, Örnek Caddesi No:123",
    "items": [...]
  },
  "message": "Sipariş durumu 'Processing' olarak güncellendi"
}
```

---

### 5. Tüm Siparişleri Listele (Admin Only)
**GET** `/api/orders/admin/all`

Admin paneli için tüm siparişleri filtreleme ve sırlama seçenekleri ile listeler.

**Gerekli:** Authorization (JWT Token + Admin Role)

**Query Parameters:**
- `status` (string, optional) - Durum filtresi
- `startDate` (datetime, optional) - Başlangıç tarihi
- `endDate` (datetime, optional) - Bitiş tarihi
- `userID` (int, optional) - Kullanıcı ID'sine göre filtrele
- `minAmount` (decimal, optional) - Minimum tutar
- `maxAmount` (decimal, optional) - Maksimum tutar
- `pageNumber` (int, default: 1) - Sayfa numarası
- `pageSize` (int, default: 10) - Sayfa başına ürün sayısı
- `sortBy` (string, default: "OrderDate") - Sıralama: OrderDate, TotalAmount
- `sortDescending` (bool, default: true) - Azalan sırada mı?

**Example:**
```
GET /api/orders/admin/all?status=Pending&pageNumber=1&pageSize=20&sortBy=TotalAmount&sortDescending=true
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orders": [...],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 8
  },
  "message": "Tüm siparişler getirildi"
}
```

---

### 6. Siparişi İptal Et
**PATCH** `/api/orders/{id}/cancel`

Kullanıcı kendi "Pending" durumundaki siparişlerini iptal edebilir.

**Gerekli:** Authorization (JWT Token)

**URL Parameters:**
- `id` (int) - Sipariş ID'si

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "orderID": 1,
    "userID": 1,
    "userEmail": "user@example.com",
    "totalAmount": 299.99,
    "status": "Cancelled",
    "orderDate": "2025-12-06T10:30:00Z",
    "shippingAddress": "İstanbul, Avcılar, Örnek Caddesi No:123",
    "items": [...]
  },
  "message": "Sipariş başarıyla iptal edildi"
}
```

---

## Hata Yanıtları

Tüm endpointler aşağıdaki hata yapısı ile yanıt verebilir:

**400 Bad Request:**
```json
{
  "success": false,
  "message": "Sepetiniz boş",
  "errors": []
}
```

**401 Unauthorized:**
```json
{
  "success": false,
  "message": "Kullanıcı tanınmadı",
  "errors": []
}
```

**403 Forbidden:**
```json
{
  "success": false,
  "message": "Bu işlem için yetkiniz yok",
  "errors": []
}
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Sipariş bulunamadı",
  "errors": []
}
```

**500 Internal Server Error:**
```json
{
  "success": false,
  "message": "Sipariş oluşturulurken bir hata oluştu",
  "errors": []
}
```

---

## Kullanım Örneği (JavaScript/Fetch)

```javascript
// 1. Sipariş Oluştur
const createOrder = async (token, shippingAddress) => {
  const response = await fetch('https://localhost:7100/api/orders', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ shippingAddress })
  });
  return response.json();
};

// 2. Siparişleri Al
const getMyOrders = async (token, pageNumber = 1) => {
  const response = await fetch(
    `https://localhost:7100/api/orders?pageNumber=${pageNumber}`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );
  return response.json();
};

// 3. Sipariş Detayını Al
const getOrderDetails = async (token, orderId) => {
  const response = await fetch(`https://localhost:7100/api/orders/${orderId}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

// 4. Sipariş Durumunu Güncelle (Admin)
const updateOrderStatus = async (token, orderId, status) => {
  const response = await fetch(`https://localhost:7100/api/orders/${orderId}/status`, {
    method: 'PATCH',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ status })
  });
  return response.json();
};

// 5. Siparişi İptal Et
const cancelOrder = async (token, orderId) => {
  const response = await fetch(`https://localhost:7100/api/orders/${orderId}/cancel`, {
    method: 'PATCH',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};
```

---

## Notlar

- ✅ Tüm tarih/saat değerleri UTC formatında döndürülür
- ✅ Siparişler oluşturulduktan sonra sepet otomatik olarak boşaltılır
- ✅ Stok otomatik olarak güncellenir
- ✅ İptal edilen siparişlerin stoku geri eklenir
- ✅ Sadece "Pending" durumundaki siparişler iptal edilebilir
