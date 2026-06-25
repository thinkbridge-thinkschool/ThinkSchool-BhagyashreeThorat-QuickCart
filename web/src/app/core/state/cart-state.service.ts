import { Injectable, inject, signal } from '@angular/core';
import { CartApi } from '../api/cart.service';
import { Cart } from '../models';

/**
 * Central cart count state. Written by every component that mutates the cart
 * (add / update / remove / checkout). Read by the navbar badge and any component
 * that needs the live count. Backed by a signal so the navbar re-renders automatically.
 */
@Injectable({ providedIn: 'root' })
export class CartState {
  private readonly api = inject(CartApi);

  private readonly _count = signal(0);

  /** Total number of items (sum of all quantities) across all cart lines. */
  readonly count = this._count.asReadonly();

  /** Sync the badge from a full Cart response returned by any mutation. */
  setFromCart(cart: Cart): void {
    this._count.set(cart.items.reduce((sum, i) => sum + i.quantity, 0));
  }

  /** Fetch the current cart and refresh the badge. Called once after authentication. */
  refresh(): void {
    this.api.get().subscribe({
      next: (cart) => this.setFromCart(cart),
      error: () => {}, // silent — user not authenticated or cart is empty
    });
  }
}
