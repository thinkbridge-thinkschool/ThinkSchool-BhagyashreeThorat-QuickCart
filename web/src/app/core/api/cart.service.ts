import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Cart } from '../models';

@Injectable({ providedIn: 'root' })
export class CartApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/cart`;

  get(): Observable<Cart> {
    return this.http.get<Cart>(this.base);
  }

  addItem(productId: string, quantity: number): Observable<Cart> {
    return this.http.post<Cart>(`${this.base}/items`, { productId, quantity });
  }

  updateItem(productId: string, quantity: number): Observable<Cart> {
    return this.http.put<Cart>(`${this.base}/items/${productId}`, { quantity });
  }

  removeItem(productId: string): Observable<Cart> {
    return this.http.delete<Cart>(`${this.base}/items/${productId}`);
  }
}
