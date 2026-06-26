// TypeScript shapes mirroring the QuickCart API response/request contracts.

export interface Category {
  categoryId: string;
  categoryName: string;
  description?: string | null;
}

export interface Product {
  productId: string;
  categoryId: string;
  productName: string;
  description?: string | null;
  price: number;
  imageUrl?: string | null;
  stockQuantity: number;
  isAvailable: boolean;
}

export interface CartItem {
  productId: string;
  productName: string;
  imageUrl?: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  isAvailable: boolean;
}

export interface Cart {
  cartId: string;
  items: CartItem[];
  total: number;
}

export interface OrderItem {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  orderId: string;
  userId: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
  items: OrderItem[];
}

export interface UserProfile {
  userId: string;
  email: string;
  displayName?: string | null;
  phoneNumber?: string | null;
}

/** Paginated API response envelope — mirrors PagedResponse<T> on the backend. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
