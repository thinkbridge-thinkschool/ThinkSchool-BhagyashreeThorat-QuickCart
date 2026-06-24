import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CatalogApi } from '../../core/api/catalog.service';
import { Category, Product } from '../../core/models';
import { CategoryStrip } from './category-strip/category-strip.component';
import { ProductSection } from './product-section/product-section.component';

export interface ProductGroup {
  category: Category;
  products: Product[];
}

@Component({
  selector: 'app-home',
  imports: [CategoryStrip, ProductSection],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly catalog = inject(CatalogApi);
  private readonly router = inject(Router);

  readonly categories = signal<Category[]>([]);
  readonly allProducts = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly selectedCategoryId = signal<string | null>(null);

  /** Products grouped by category, respecting the active category filter. */
  readonly sections = computed<ProductGroup[]>(() => {
    const sel = this.selectedCategoryId();
    const cats = sel
      ? this.categories().filter((c) => c.categoryId === sel)
      : this.categories();

    return cats
      .map((cat) => ({
        category: cat,
        products: this.allProducts().filter((p) => p.categoryId === cat.categoryId),
      }))
      .filter((g) => g.products.length > 0);
  });

  ngOnInit(): void {
    this.catalog.getCategories().subscribe((cats) => this.categories.set(cats));
    this.catalog.getProducts().subscribe({
      next: (prods) => { this.allProducts.set(prods); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  onCategorySelect(id: string | null): void {
    this.selectedCategoryId.set(id);
  }

  onSeeAll(categoryId: string): void {
    void this.router.navigate(['/products'], { queryParams: { categoryId } });
  }
}
