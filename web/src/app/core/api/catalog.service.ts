import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Category, Product } from '../models';

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.base}/categories`);
  }

  getProducts(opts?: { search?: string; categoryId?: string }): Observable<Product[]> {
    let params = new HttpParams();
    if (opts?.search) params = params.set('search', opts.search);
    if (opts?.categoryId) params = params.set('categoryId', opts.categoryId);
    return this.http.get<Product[]>(`${this.base}/products`, { params });
  }

  getProduct(productId: string): Observable<Product> {
    return this.http.get<Product>(`${this.base}/products/${productId}`);
  }
}
