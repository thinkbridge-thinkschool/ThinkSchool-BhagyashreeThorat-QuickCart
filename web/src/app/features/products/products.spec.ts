import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Products, PRODUCT_PLACEHOLDER } from './products';
import { SearchState } from '../../core/state/search.service';

describe('Products', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Products],
      providers: [provideHttpClient(), provideRouter([])],
    });
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Products);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should expose the SVG placeholder constant', () => {
    expect(PRODUCT_PLACEHOLDER).toContain('data:image/svg+xml');
  });

  it('should start with loading true and no products', () => {
    const fixture = TestBed.createComponent(Products);
    const component = fixture.componentInstance;
    // Before ngOnInit the loading flag defaults to false, allProducts is empty.
    expect(component.allProducts()).toEqual([]);
    expect(component.selectedCategory()).toBeNull();
  });

  it('should filter products by search term via SearchState', () => {
    const fixture = TestBed.createComponent(Products);
    const component = fixture.componentInstance;
    const search = TestBed.inject(SearchState);

    // Seed some products directly on the signal.
    component.allProducts.set([
      { productId: '1', categoryId: 'c1', productName: 'Basmati Rice', description: null, price: 12.99, imageUrl: null, stockQuantity: 10, isAvailable: true },
      { productId: '2', categoryId: 'c1', productName: 'Potato Chips', description: null, price: 1.99, imageUrl: null, stockQuantity: 50, isAvailable: true },
    ]);

    search.set('rice');
    expect(component.filtered().length).toBe(1);
    expect(component.filtered()[0].productName).toBe('Basmati Rice');

    search.clear();
    expect(component.filtered().length).toBe(2);
  });

  it('should filter products by selected category', () => {
    const fixture = TestBed.createComponent(Products);
    const component = fixture.componentInstance;

    component.allProducts.set([
      { productId: '1', categoryId: 'cat-a', productName: 'Rice', description: null, price: 1, imageUrl: null, stockQuantity: 1, isAvailable: true },
      { productId: '2', categoryId: 'cat-b', productName: 'Chips', description: null, price: 1, imageUrl: null, stockQuantity: 1, isAvailable: true },
    ]);

    component.selectCategory('cat-a');
    expect(component.filtered().length).toBe(1);
    expect(component.filtered()[0].categoryId).toBe('cat-a');

    component.selectCategory(null);
    expect(component.filtered().length).toBe(2);
  });
});
