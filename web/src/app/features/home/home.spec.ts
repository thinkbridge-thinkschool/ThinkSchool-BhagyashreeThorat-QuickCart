import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { Home, ProductGroup } from './home';
import { Category, Product } from '../../core/models';

const CATS: Category[] = [
  { categoryId: 'c1', categoryName: 'Snacks', description: null },
  { categoryId: 'c2', categoryName: 'Beverages', description: null },
];
const PRODS: Product[] = [
  { productId: 'p1', categoryId: 'c1', productName: 'Chips', description: null, price: 1.99, imageUrl: null, stockQuantity: 10, isAvailable: true },
  { productId: 'p2', categoryId: 'c2', productName: 'Juice', description: null, price: 3.20, imageUrl: null, stockQuantity: 10, isAvailable: true },
];

describe('Home', () => {
  let fixture: ComponentFixture<Home>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Home],
      providers: [provideHttpClient(), provideRouter([])],
    });
    fixture = TestBed.createComponent(Home);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('sections() should return all groups when no category is selected', () => {
    const comp = fixture.componentInstance;
    comp.categories.set(CATS);
    comp.allProducts.set(PRODS);
    comp.selectedCategoryId.set(null);

    const groups = comp.sections();
    expect(groups.length).toBe(2);
  });

  it('sections() should filter to one group when a category is selected', () => {
    const comp = fixture.componentInstance;
    comp.categories.set(CATS);
    comp.allProducts.set(PRODS);
    comp.selectedCategoryId.set('c1');

    const groups = comp.sections();
    expect(groups.length).toBe(1);
    expect(groups[0].category.categoryId).toBe('c1');
  });

  it('sections() should exclude categories with no products', () => {
    const comp = fixture.componentInstance;
    comp.categories.set([...CATS, { categoryId: 'c3', categoryName: 'Empty', description: null }]);
    comp.allProducts.set(PRODS);
    comp.selectedCategoryId.set(null);

    const groups = comp.sections();
    expect(groups.length).toBe(2); // c3 has no products, excluded
  });
});
