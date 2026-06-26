import { Component, input, output } from '@angular/core';
import { Category, Product } from '../../../core/models';
import { ProductCard } from '../../../shared/product-card/product-card';

/**
 * One category section on the Home page: a header with "See All →" and a
 * horizontal scrolling carousel of ProductCard tiles.
 *
 * Cart interaction, routing, and image fallback are all handled inside ProductCard;
 * this component is responsible only for the section layout.
 */
@Component({
  selector: 'app-product-section',
  imports: [ProductCard],
  templateUrl: './product-section.component.html',
  styleUrl: './product-section.component.css',
})
export class ProductSection {
  readonly category = input.required<Category>();
  readonly products = input.required<Product[]>();

  /** Emits the categoryId when the user clicks "See All →". */
  readonly seeAll = output<string>();

  onSeeAll(): void {
    this.seeAll.emit(this.category().categoryId);
  }
}
