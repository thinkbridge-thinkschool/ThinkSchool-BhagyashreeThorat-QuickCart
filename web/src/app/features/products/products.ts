import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { debounceTime, skip } from 'rxjs';
import { CatalogApi } from '../../core/api/catalog.service';
import { SearchState } from '../../core/state/search.service';
import { Category, PagedResult, Product } from '../../core/models';
import { CategoryImageStrip } from '../../shared/category-image-strip/category-image-strip.component';
import { ProductCard } from '../../shared/product-card/product-card';

// Re-export so existing importers (product-detail.ts) continue to work.
export { PRODUCT_PLACEHOLDER } from '../../shared/product-card/product-card';

@Component({
  selector: 'app-products',
  imports: [CategoryImageStrip, ProductCard],
  templateUrl: './products.html',
  styleUrl: './products.css',
})
export class Products implements OnInit {
  private readonly catalog    = inject(CatalogApi);
  private readonly search     = inject(SearchState);
  private readonly route      = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  // ── State ────────────────────────────────────────────────────────────
  readonly categories       = signal<Category[]>([]);
  readonly pagedResult      = signal<PagedResult<Product> | null>(null);
  readonly loading          = signal(false);
  readonly selectedCategory = signal<string | null>(null);
  readonly toast            = signal<string | null>(null);
  readonly page             = signal(1);
  readonly pageSize         = 12;

  // ── Derived from paged result ─────────────────────────────────────────
  readonly items       = computed(() => this.pagedResult()?.items ?? []);
  readonly totalPages  = computed(() => this.pagedResult()?.totalPages ?? 0);
  readonly hasPrevious = computed(() => this.pagedResult()?.hasPrevious ?? false);
  readonly hasNext     = computed(() => this.pagedResult()?.hasNext ?? false);
  readonly totalItems  = computed(() => this.pagedResult()?.totalItems ?? 0);

  private readonly categoryById = computed(() => {
    const map = new Map<string, string>();
    for (const c of this.categories()) map.set(c.categoryId, c.categoryName);
    return map;
  });

  constructor() {
    // toObservable requires an injection context — constructor qualifies, ngOnInit does not.
    toObservable(this.search.term).pipe(
      skip(1),
      debounceTime(350),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(() => {
      this.page.set(1);
      this.loadPage();
    });
  }

  ngOnInit(): void {
    const catParam = this.route.snapshot.queryParamMap.get('categoryId');
    if (catParam) this.selectedCategory.set(catParam);

    this.catalog.getCategories().subscribe((c) => this.categories.set(c));
    this.loadPage();
  }

  // ── Public actions ───────────────────────────────────────────────────

  /** Returns the display name for a category id — passed to ProductCard as [categoryName]. */
  categoryName(id: string): string {
    return this.categoryById().get(id) ?? '';
  }

  /** Called by CategoryStrip's (categorySelect) output. */
  selectCategory(id: string | null): void {
    this.selectedCategory.set(id);
    this.page.set(1);
    this.loadPage();
  }

  goToPage(p: number): void {
    this.page.set(p);
    this.loadPage();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // ── Private helpers ───────────────────────────────────────────────────

  private loadPage(): void {
    this.loading.set(true);

    this.catalog.getProducts({
      page:       this.page(),
      pageSize:   this.pageSize,
      search:     this.search.term().trim() || undefined,
      categoryId: this.selectedCategory() ?? undefined,
    }).subscribe({
      next:  (result) => { this.pagedResult.set(result); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.flash('Could not load products. Check that the API is running.');
      },
    });
  }

  private flash(msg: string): void {
    this.toast.set(msg);
    setTimeout(() => this.toast.set(null), 2000);
  }
}
