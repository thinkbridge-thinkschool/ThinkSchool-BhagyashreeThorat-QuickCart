import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { CatalogApi } from '../../core/api/catalog.service';
import { CartApi } from '../../core/api/cart.service';
import { CartState } from '../../core/state/cart-state.service';
import { AuthService } from '../../core/auth/auth.service';
import { Category, Product } from '../../core/models';
import { PRODUCT_PLACEHOLDER } from '../../shared/product-card/product-card';

@Component({
  selector: 'app-product-detail',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css',
})
export class ProductDetail implements OnInit {
  private readonly catalog   = inject(CatalogApi);
  private readonly cart      = inject(CartApi);
  private readonly cartState = inject(CartState);
  private readonly auth      = inject(AuthService);
  private readonly route     = inject(ActivatedRoute);

  readonly placeholder = PRODUCT_PLACEHOLDER;

  // ── Async state ──────────────────────────────────────────────────────
  readonly product    = signal<Product | null>(null);
  readonly categories = signal<Category[]>([]);
  readonly loading    = signal(true);
  readonly notFound   = signal(false);
  readonly error      = signal<string | null>(null);

  // ── Interaction state ────────────────────────────────────────────────
  readonly qty     = signal(1);
  readonly adding  = signal(false);
  readonly toast   = signal<string | null>(null);

  // ── Derived ──────────────────────────────────────────────────────────
  /** Category name resolved from the product's categoryId once both signals are populated. */
  readonly categoryName = computed(() => {
    const p = this.product();
    if (!p) return '';
    return this.categories().find((c) => c.categoryId === p.categoryId)?.categoryName ?? '';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      this.notFound.set(true);
      return;
    }

    // Fire both requests in parallel. Categories are only needed for the name label;
    // a failure there should not prevent the product details from showing.
    this.catalog.getProduct(id).subscribe({
      next: (p) => {
        this.product.set(p);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.notFound.set(true);
        } else {
          this.error.set('Failed to load product details. Please try again.');
        }
      },
    });

    this.catalog.getCategories().subscribe({
      next: (cats) => this.categories.set(cats),
      // Non-critical: silently skip the category label on failure.
    });
  }

  // ── Quantity selector ─────────────────────────────────────────────────

  incrementQty(): void {
    const max = this.product()?.stockQuantity ?? 1;
    this.qty.update((q) => Math.min(q + 1, max));
  }

  decrementQty(): void {
    this.qty.update((q) => Math.max(q - 1, 1));
  }

  // ── Cart ──────────────────────────────────────────────────────────────

  add(): void {
    const p = this.product();
    if (!p || !p.isAvailable) return;

    if (!this.auth.isAuthenticated) {
      void this.auth.login();
      return;
    }

    this.adding.set(true);
    this.cart.addItem(p.productId, this.qty()).subscribe({
      next: (c) => {
        this.cartState.setFromCart(c);
        this.adding.set(false);
        this.flash(`Added ${this.qty()} × ${p.productName} to cart`);
      },
      error: () => {
        this.adding.set(false);
        this.flash('Could not add to cart. Please try again.');
      },
    });
  }

  onImgError(e: Event): void {
    (e.target as HTMLImageElement).src = this.placeholder;
  }

  private flash(msg: string): void {
    this.toast.set(msg);
    setTimeout(() => this.toast.set(null), 2500);
  }
}
