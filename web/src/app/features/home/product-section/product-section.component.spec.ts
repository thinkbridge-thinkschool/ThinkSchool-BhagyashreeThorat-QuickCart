import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { By } from '@angular/platform-browser';
import { ProductSection, SECTION_PLACEHOLDER } from './product-section.component';
import { Category, Product } from '../../../core/models';

const CAT: Category = { categoryId: 'c1', categoryName: 'Snacks', description: null };
const PRODS: Product[] = [
  { productId: 'p1', categoryId: 'c1', productName: 'Potato Chips 150g', description: null, price: 1.99, imageUrl: null, stockQuantity: 50, isAvailable: true },
  { productId: 'p2', categoryId: 'c1', productName: 'Chocolate Biscuits 300g', description: null, price: 2.30, imageUrl: null, stockQuantity: 100, isAvailable: true },
];

describe('ProductSection', () => {
  let fixture: ComponentFixture<ProductSection>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductSection],
      providers: [provideHttpClient()],
    });
    fixture = TestBed.createComponent(ProductSection);
    fixture.componentRef.setInput('category', CAT);
    fixture.componentRef.setInput('products', PRODS);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the category name as section title', () => {
    const title = fixture.debugElement.query(By.css('.ps-title'));
    expect(title.nativeElement.textContent.trim()).toBe('Snacks');
  });

  it('should render a card for every product', () => {
    const cards = fixture.debugElement.queryAll(By.css('.ps-card'));
    expect(cards.length).toBe(PRODS.length);
  });

  it('should emit seeAll with the category id when "See All" is clicked', () => {
    let emitted: string | undefined;
    fixture.componentInstance.seeAll.subscribe((id) => (emitted = id));
    fixture.debugElement.query(By.css('.ps-see-all')).triggerEventHandler('click', null);
    expect(emitted).toBe('c1');
  });

  it('should export the placeholder constant', () => {
    expect(SECTION_PLACEHOLDER).toContain('data:image/svg+xml');
  });
});
