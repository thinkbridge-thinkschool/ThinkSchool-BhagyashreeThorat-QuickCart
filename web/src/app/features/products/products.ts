import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { CatalogApi } from '../../core/api/catalog.service';
import { CartApi } from '../../core/api/cart.service';
import { AuthService } from '../../core/auth/auth.service';
import { SearchState } from '../../core/state/search.service';
import { CartState } from '../../core/state/cart-state.service';
import { Category, Product } from '../../core/models';

export const PRODUCT_PLACEHOLDER =
  'data:image/svg+xml;utf8,' +
  encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="300" height="200">
       <rect width="300" height="200" fill="#eef1f6"/>
       <g fill="none" stroke="#c2cad6" stroke-width="6" stroke-linejoin="round" stroke-linecap="round">
         <path d="M110 82 h80 l-8 66 a6 6 0 0 1 -6 6 h-52 a6 6 0 0 1 -6 -6 z"/>
         <path d="M128 82 a22 22 0 0 1 44 0"/>
       </g>
     </svg>`,
  );

@Component({
  selector: 'app-products',
  imports: [CurrencyPipe],
  templateUrl: './products.html',
  styleUrl: './products.css',
})
export class Products implements OnInit {
  private readonly catalog = inject(CatalogApi);
  private readonly cart = inject(CartApi);
  private readonly cartState = inject(CartState);
  private readonly auth = inject(AuthService);
  private readonly search = inject(SearchState);
  private readonly route = inject(ActivatedRoute);

  readonly placeholder = PRODUCT_PLACEHOLDER;

  readonly categories = signal<Category[]>([]);
  readonly allProducts = signal<Product[]>([]);
  readonly loading = signal(false);
  readonly selectedCategory = signal<string | null>(null);
  readonly toast = signal<string | null>(null);

  private readonly categoryById = computed(() => {
    const map = new Map<string, string>();
    for (const c of this.categories()) map.set(c.categoryId, c.categoryName);
    return map;
  });

  readonly filtered = computed(() => {
    const term = this.search.term().trim().toLowerCase();
    const cat = this.selectedCategory();
    return this.allProducts().filter((p) => {
      if (cat && p.categoryId !== cat) return false;
      if (term) {
        const hay = `${p.productName} ${p.description ?? ''}`.toLowerCase();
        if (!hay.includes(term)) return false;
      }
      return true;
    });
  });

  ngOnInit(): void {
    // Pre-select a category when navigated here from "See All".
    const catParam = this.route.snapshot.queryParamMap.get('categoryId');
    if (catParam) this.selectedCategory.set(catParam);

    this.catalog.getCategories().subscribe((c) => this.categories.set(c));

    this.loading.set(true);
    this.catalog.getProducts().subscribe({
      next: (p) => { this.allProducts.set(p); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  categoryName(id: string): string {
    return this.categoryById().get(id) ?? '';
  }

  selectCategory(id: string | null): void {
    this.selectedCategory.set(id);
  }

  add(p: Product): void {
    if (!this.auth.isAuthenticated) {
      void this.auth.login();
      return;
    }
    this.cart.addItem(p.productId, 1).subscribe({
      next: (cart) => {
        this.cartState.setFromCart(cart); // keep navbar badge in sync
        this.flash(`Added ${p.productName} to cart`);
      },
    });
  }

  onImgError(e: Event): void {
    (e.target as HTMLImageElement).src = PRODUCT_PLACEHOLDER;
  }

  private flash(msg: string): void {
    this.toast.set(msg);
    setTimeout(() => this.toast.set(null), 2000);
  }
}
