import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order } from '../models';

@Injectable({ providedIn: 'root' })
export class OrderApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/orders`;

  /** Checkout the current cart (no body — the server reads the cart). */
  checkout(): Observable<Order> {
    return this.http.post<Order>(this.base, {});
  }

  getMine(): Observable<Order[]> {
    return this.http.get<Order[]>(this.base);
  }

  getById(orderId: string): Observable<Order> {
    return this.http.get<Order>(`${this.base}/${orderId}`);
  }

  cancel(orderId: string): Observable<Order> {
    return this.http.post<Order>(`${this.base}/${orderId}/cancel`, {});
  }
}
