const API_BASE = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7100';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ProductSummary {
  productID: number;
  productName: string;
  brand: string;
  price: number;
  imageUrl: string;
  categoryName?: string;
  stock: number;
}

export interface ProductDetail extends ProductSummary {
  description: string;
  categoryID: number;
  createdAt: string;
}

export interface ProductSpecification {
  specID: number;
  specName: string;
  specValue: string;
}

export interface Review {
  productReviewID: number;
  userName: string;
  rating: number;
  reviewText?: string;
  reviewDate: string;
  isVerifiedPurchase: boolean;
}

export interface CartItem {
  cartItemID: number;
  productID: number;
  productName: string;
  price: number;
  count: number;
  productImageUrl?: string;
  subtotal: number;
}

export interface CartSummary {
  items: CartItem[];
  totalItems: number;
  totalPrice: number;
}

export interface FavoriteItem {
  favoriteID: number;
  productID: number;
  productName: string;
  brand: string;
  price: number;
  imageUrl?: string;
  stock: number;
  categoryName?: string;
}

export interface FavoriteAction {
  success: boolean;
  message: string;
  productID: number;
}

export interface AdminStats {
  totalProducts: number;
  activeProducts: number;
  outOfStockProducts: number;
  totalCategories: number;
  totalOrders: number;
  pendingOrders: number;
  completedOrders: number;
  cancelledOrders: number;
}

export interface OrderItem {
  orderItemID: number;
  productID: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  subtotal: number;
}

export interface Order {
  orderID: number;
  userEmail: string;
  totalAmount: number;
  status: string;
  orderDate: string;
  shippingAddress: string;
  items: OrderItem[];
}

export interface UserProfile {
  id?: number;
  email: string;
  firstName: string;
  lastName: string;
  address?: string;
  roles?: string[];
}

export interface Category {
  categoryID: number;
  categoryName: string;
  parentCategoryID?: number | null;
  subCategories?: Category[];
}

export interface Brand {
  brandID: number;
  brandName: string;
  productCount: number;
}

type FetchOptions = RequestInit & { token?: string | null };

async function apiFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  const headers = new Headers(options.headers as HeadersInit);
  headers.set('Content-Type', 'application/json');

  if (options.token) {
    headers.set('Authorization', `Bearer ${options.token}`);
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  const json = await response.json();
  if (!response.ok) {
    const message = json?.message || 'Beklenmeyen bir hata oluştu';
    throw new Error(message);
  }
  return json;
}

function unpackPaged<T>(data: any): PagedResult<T> {
  return {
    items: data.items || data.Items || [],
    totalCount: data.totalCount ?? data.TotalCount ?? 0,
    pageNumber: data.pageNumber ?? data.PageNumber ?? 1,
    pageSize: data.pageSize ?? data.PageSize ?? 10,
    totalPages: data.totalPages ?? data.TotalPages ?? 1,
  };
}

export const api = {
  async login(email: string, password: string) {
    return apiFetch<{ accessToken: string; refreshToken?: string }>('/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  },
  async register(payload: { firstName: string; lastName: string; email: string; password: string }) {
    return apiFetch<ApiResponse<null>>('/api/accounts/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },
  async getProfile(token: string) {
    const res = await apiFetch<ApiResponse<any>>('/api/accounts/profile', { token });
    const data = res.data;
    if (!data) throw new Error('Kullanıcı bilgisi alınamadı');
    return {
      id: data.id ?? data.Id,
      email: data.email ?? data.Email,
      firstName: data.firstName ?? data.FirstName,
      lastName: data.lastName ?? data.LastName,
      address: data.address ?? data.Address,
      roles: data.roles ?? data.Roles ?? [],
    } as UserProfile;
  },
  async getProducts(params: Record<string, string | number | boolean | undefined> = {}) {
    const query = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value === undefined || value === '' || value === null) return;
      query.append(key, String(value));
    });
    const qs = query.toString();
    const res = await apiFetch<ApiResponse<PagedResult<ProductSummary>>>(
      `/api/products${qs ? `?${qs}` : ''}`,
    );
    if (!res.data) throw new Error('Ürün listesi alınamadı');
    return unpackPaged<ProductSummary>(res.data);
  },
  async getProductDetail(id: number) {
    const res = await apiFetch<ApiResponse<ProductDetail>>(`/api/products/${id}`);
    if (!res.data) throw new Error('Ürün detayı bulunamadı');
    return res.data;
  },
  async getProductSpecifications(id: number) {
    const res = await apiFetch<ApiResponse<ProductSpecification[]>>(
      `/api/products/${id}/specifications`,
    );
    return res.data || [];
  },
  async getRelatedProducts(id: number) {
    const res = await apiFetch<ApiResponse<ProductSummary[]>>(`/api/products/${id}/related`);
    return res.data || [];
  },
  async getReviews(productId: number) {
    const res = await apiFetch<ApiResponse<Review[]>>(`/api/products/${productId}/reviews`);
    return res.data || [];
  },
  async addReview(productId: number, rating: number, reviewText: string, token: string) {
    return apiFetch<ApiResponse<Review>>(`/api/products/${productId}/reviews`, {
      method: 'POST',
      token,
      body: JSON.stringify({ rating, reviewText }),
    });
  },
  async toggleFavorite(productId: number, token: string) {
    return apiFetch<ApiResponse<FavoriteAction>>(`/api/favorites/${productId}`, {
      method: 'POST',
      token,
    });
  },
  async getFavorites(token: string, pageNumber = 1, pageSize = 10) {
    const res = await apiFetch<ApiResponse<any>>(
      `/api/favorites?pageNumber=${pageNumber}&pageSize=${pageSize}`,
      { token },
    );
    const data = res.data;
    return {
      items: data?.items || data?.Items || data?.data || data?.Data || [],
      totalCount: data?.totalCount ?? data?.TotalCount ?? data?.totalCount ?? 0,
    };
  },
  async isFavorite(productId: number, token: string) {
    const res = await apiFetch<ApiResponse<{ productID: number; isFavorite: boolean }>>(
      `/api/favorites/${productId}/check`,
      { token },
    );
    return res.data?.isFavorite ?? false;
  },
  async getCart(token: string) {
    const res = await apiFetch<ApiResponse<CartSummary>>('/api/cart', { token });
    if (!res.data) throw new Error('Sepet alınamadı');
    return res.data;
  },
  async addToCart(productID: number, count: number, token: string) {
    return apiFetch<ApiResponse<CartSummary>>('/api/cart/add', {
      method: 'POST',
      token,
      body: JSON.stringify({ productID, count }),
    });
  },
  async updateCartItem(itemId: number, count: number, token: string) {
    return apiFetch<ApiResponse<CartSummary>>(`/api/cart/${itemId}`, {
      method: 'PATCH',
      token,
      body: JSON.stringify({ count }),
    });
  },
  async removeCartItem(itemId: number, token: string) {
    return apiFetch<ApiResponse<CartSummary>>(`/api/cart/${itemId}`, {
      method: 'DELETE',
      token,
    });
  },
  async clearCart(token: string) {
    return apiFetch<ApiResponse<CartSummary>>('/api/cart', { method: 'DELETE', token });
  },
  async createOrder(shippingAddress: string, token: string) {
    return apiFetch<ApiResponse<Order>>('/api/orders', {
      method: 'POST',
      token,
      body: JSON.stringify({ shippingAddress }),
    });
  },
  async getOrders(token: string) {
    const res = await apiFetch<ApiResponse<any>>('/api/orders', { token });
    const data = res.data;
    if (!data) throw new Error('Siparişler alınamadı');
    return {
      items: data.orders || data.Items || [],
      totalCount: data.totalCount ?? data.TotalCount ?? 0,
      pageNumber: data.pageNumber ?? data.PageNumber ?? 1,
      pageSize: data.pageSize ?? data.PageSize ?? 10,
      totalPages: data.totalPages ?? data.TotalPages ?? 1,
    } as PagedResult<Order>;
  },
  async getAdminStats(token: string) {
    const res = await apiFetch<ApiResponse<AdminStats>>('/api/admin/stats', { token });
    if (!res.data) throw new Error('İstatistikler alınamadı');
    return res.data;
  },
  async getCategories() {
    const res = await apiFetch<ApiResponse<Category[]>>('/api/categories');
    return res.data || [];
  },
  async getBrands() {
    const res = await apiFetch<ApiResponse<Brand[]>>('/api/brands');
    return res.data || [];
  },
};
