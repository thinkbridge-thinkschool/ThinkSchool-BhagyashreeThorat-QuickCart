import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderApi } from '../../core/api/order.service';
import { Order } from '../../core/models';

@Component({
  selector: 'app-order-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.css',
})
export class OrderDetail implements OnInit {
  private readonly api = inject(OrderApi);
  private readonly route = inject(ActivatedRoute);

  readonly order = signal<Order | null>(null);
  readonly loading = signal(true);
  readonly cancelling = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.getById(id).subscribe({
      next: (o) => { this.order.set(o); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  cancel(): void {
    const current = this.order();
    if (!current) return;
    if (!confirm('Cancel this order? This cannot be undone.')) return;

    this.cancelling.set(true);
    this.api.cancel(current.orderId).subscribe({
      next: (o) => { this.order.set(o); this.cancelling.set(false); },
      error: (e) => {
        this.cancelling.set(false);
        const msg = e?.error?.detail ?? 'Could not cancel the order.';
        this.error.set(msg);
        setTimeout(() => this.error.set(null), 3000);
      },
    });
  }
}
