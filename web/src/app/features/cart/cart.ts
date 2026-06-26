import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Router } from '@angular/router';
import { CartApi } from '../../core/api/cart.service';
import { OrderApi } from '../../core/api/order.service';
import { CartState } from '../../core/state/cart-state.service';
import { Cart as CartModel } from '../../core/models';

@Component({
  selector: 'app-cart',
  imports: [CurrencyPipe],
  templateUrl: './cart.html',
  styleUrl: './cart.css',
})
export class Cart implements OnInit {
  private readonly api = inject(CartApi);
  private readonly orders = inject(OrderApi);
  private readonly cartState = inject(CartState);
  private readonly router = inject(Router);

  readonly cart = signal<CartModel | null>(null);
  readonly loading = signal(true);
  readonly placing = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  private reload(): void {
    this.loading.set(true);
    this.api.get().subscribe({
      next: (c) => {
        this.cart.set(c);
        this.cartState.setFromCart(c); // sync badge on page load
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  changeQty(productId: string, quantity: number): void {
    if (quantity < 1) return;
    this.api.updateItem(productId, quantity).subscribe((c) => {
      this.cart.set(c);
      this.cartState.setFromCart(c);
    });
  }

  remove(productId: string): void {
    this.api.removeItem(productId).subscribe((c) => {
      this.cart.set(c);
      this.cartState.setFromCart(c);
    });
  }

  checkout(): void {
    if (!confirm('Place this order? You can cancel it from the Orders page if needed.')) return;
    this.placing.set(true);
    this.orders.checkout().subscribe({
      next: (order) => {
        this.cartState.setFromCart({ cartId: '', items: [], total: 0 }); // badge → 0
        void this.router.navigate(['/orders', order.orderId]);
      },
      error: (e) => {
        this.placing.set(false);
        this.flash(e?.error?.detail ?? 'Checkout failed.');
      },
    });
  }

  private flash(msg: string): void {
    this.error.set(msg);
    setTimeout(() => this.error.set(null), 2500);
  }
}
