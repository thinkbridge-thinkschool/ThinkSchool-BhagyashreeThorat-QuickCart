import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class OrderApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/orders`;

  /** Checkout the current cart (no body — the server reads the cart). */
  checkout(): Observable<Order> {
    return this.http.post<Order>(this.base, {});
  }

  /** Returns a paginated page of the current user's orders, newest first. */
  getMine(opts?: { page?: number; pageSize?: number }): Observable<PagedResult<Order>> {
    let params = new HttpParams();
    if (opts?.page)     params = params.set('page',     opts.page.toString());
    if (opts?.pageSize) params = params.set('pageSize', opts.pageSize.toString());
    return this.http.get<PagedResult<Order>>(this.base, { params });
  }

  getById(orderId: string): Observable<Order> {
    return this.http.get<Order>(`${this.base}/${orderId}`);
  }

  cancel(orderId: string): Observable<Order> {
    return this.http.post<Order>(`${this.base}/${orderId}/cancel`, {});
  }
}
