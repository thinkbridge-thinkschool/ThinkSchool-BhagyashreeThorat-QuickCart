import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Cart } from './cart';

describe('Cart', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Cart],
      providers: [provideHttpClient(), provideRouter([])],
    });
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Cart);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should start with loading true and no cart', () => {
    const fixture = TestBed.createComponent(Cart);
    const component = fixture.componentInstance;
    expect(component.loading()).toBeTrue();
    expect(component.cart()).toBeNull();
    expect(component.placing()).toBeFalse();
    expect(component.error()).toBeNull();
  });

  it('should not decrement qty below 1', () => {
    const fixture = TestBed.createComponent(Cart);
    const component = fixture.componentInstance;

    // Set a cart with one item at qty 1.
    component.cart.set({
      cartId: 'c1',
      items: [{ productId: 'p1', productName: 'Rice', imageUrl: null, unitPrice: 5, quantity: 1, lineTotal: 5, isAvailable: true }],
      total: 5,
    });

    // changeQty with quantity < 1 is a no-op; the cart stays unchanged.
    const spy = spyOn<any>(component['api'], 'updateItem').and.callThrough();
    component.changeQty('p1', 0);
    expect(spy).not.toHaveBeenCalled();
  });
});
