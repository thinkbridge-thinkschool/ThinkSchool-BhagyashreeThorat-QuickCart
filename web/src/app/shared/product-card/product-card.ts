import { Component, inject, input, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartApi } from '../../core/api/cart.service';
import { CartState } from '../../core/state/cart-state.service';
import { AuthService } from '../../core/auth/auth.service';
import { Product } from '../../core/models';

/**
 * Shared SVG placeholder used wherever a product image is unavailable.
 * Exported so the Product Details page (which uses its own large-format layout)
 * can import it as the single source of truth.
 */
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

/**
 * Single product card used on the Home page carousels, the Products grid, and
 * any future surface (search results, recommendations, etc.).
 *
 * The card owns its own cart interaction so parent components do not need to
 * inject CartApi, CartState, or AuthService. Width is intentionally 100% so
 * the containing context (carousel slot, grid cell) controls the final size.
 */
@Component({
  selector: 'app-product-card',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './product-card.html',
  styleUrl: './product-card.css',
})
export class ProductCard {
  private readonly cartApi   = inject(CartApi);
  private readonly cartState = inject(CartState);
  private readonly auth      = inject(AuthService);

  /** The product to display. */
  readonly product = input.required<Product>();

  /**
   * Category label shown as a badge below the image.
   * Pass an empty string to suppress the badge (e.g., when the surrounding
   * section already makes the category obvious).
   */
  readonly categoryName = input('');

  readonly placeholder = PRODUCT_PLACEHOLDER;
  readonly toast = signal<string | null>(null);

  add(): void {
    if (!this.auth.isAuthenticated) {
      void this.auth.login();
      return;
    }
    const p = this.product();
    this.cartApi.addItem(p.productId, 1).subscribe({
      next: (cart) => {
        this.cartState.setFromCart(cart);
        this.flash(`Added ${p.productName}`);
      },
    });
  }

  onImgError(e: Event): void {
    (e.target as HTMLImageElement).src = this.placeholder;
  }

  private flash(msg: string): void {
    this.toast.set(msg);
    setTimeout(() => this.toast.set(null), 1800);
  }
}
