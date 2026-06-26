import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Category, PagedResult, Product } from '../models';

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.base}/categories`);
  }

  /**
   * Returns a paginated page of products.
   * Pass `pageSize: 200` (or similar) to fetch a large set for the home carousel
   * without adding pagination controls.
   */
  getProducts(opts?: {
    search?: string;
    categoryId?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<Product>> {
    let params = new HttpParams();
    if (opts?.search)      params = params.set('search',     opts.search);
    if (opts?.categoryId)  params = params.set('categoryId', opts.categoryId);
    if (opts?.page)        params = params.set('page',       opts.page.toString());
    if (opts?.pageSize)    params = params.set('pageSize',   opts.pageSize.toString());
    return this.http.get<PagedResult<Product>>(`${this.base}/products`, { params });
  }

  getProduct(productId: string): Observable<Product> {
    return this.http.get<Product>(`${this.base}/products/${productId}`);
  }
}
