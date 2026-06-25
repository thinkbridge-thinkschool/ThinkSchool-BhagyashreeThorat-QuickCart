import { Component, inject, input, output, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Category, Product } from '../../../core/models';
import { CartApi } from '../../../core/api/cart.service';
import { CartState } from '../../../core/state/cart-state.service';
import { AuthService } from '../../../core/auth/auth.service';

/** SVG placeholder shared with the products grid. */
export const SECTION_PLACEHOLDER =
  'data:image/svg+xml;utf8,' +
  encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="160">
       <rect width="200" height="160" fill="#eef1f6"/>
       <g fill="none" stroke="#c2cad6" stroke-width="5" stroke-linejoin="round" stroke-linecap="round">
         <path d="M74 62 h52 l-5 42a4 4 0 0 1-4 4h-34a4 4 0 0 1-4-4z"/>
         <path d="M86 62a14 14 0 0 1 28 0"/>
       </g>
     </svg>`,
  );

@Component({
  selector: 'app-product-section',
  imports: [CurrencyPipe],
  templateUrl: './product-section.component.html',
  styleUrl: './product-section.component.css',
})
export class ProductSection {
  private readonly cartApi = inject(CartApi);
  private readonly cartState = inject(CartState);
  private readonly auth = inject(AuthService);

  readonly category = input.required<Category>();
  readonly products = input.required<Product[]>();

  /** Emits the categoryId when the user clicks "See All". */
  readonly seeAll = output<string>();

  readonly placeholder = SECTION_PLACEHOLDER;
  readonly toast = signal<string | null>(null);

  onSeeAll(): void {
    this.seeAll.emit(this.category().categoryId);
  }

  add(p: Product): void {
    if (!this.auth.isAuthenticated) {
      void this.auth.login();
      return;
    }
    this.cartApi.addItem(p.productId, 1).subscribe({
      next: (cart) => {
        this.cartState.setFromCart(cart);
        this.flash(`Added ${p.productName}`);
      },
    });
  }

  onImgError(e: Event): void {
    (e.target as HTMLImageElement).src = SECTION_PLACEHOLDER;
  }

  private flash(msg: string): void {
    this.toast.set(msg);
    setTimeout(() => this.toast.set(null), 1800);
  }
}
