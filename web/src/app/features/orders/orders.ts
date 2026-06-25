import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrderApi } from '../../core/api/order.service';
import { Order } from '../../core/models';

@Component({
  selector: 'app-orders',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './orders.html',
  styleUrl: './orders.css',
})
export class Orders implements OnInit {
  private readonly api = inject(OrderApi);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getMine().subscribe({
      next: (o) => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
