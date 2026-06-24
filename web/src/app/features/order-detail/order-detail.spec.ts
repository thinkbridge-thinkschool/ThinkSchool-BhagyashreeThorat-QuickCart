import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { OrderDetail } from './order-detail';

describe('OrderDetail', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [OrderDetail],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'test-order-id' } } },
        },
      ],
    });
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(OrderDetail);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should start with loading true and no order', () => {
    const fixture = TestBed.createComponent(OrderDetail);
    const component = fixture.componentInstance;
    expect(component.loading()).toBeTrue();
    expect(component.order()).toBeNull();
    expect(component.cancelling()).toBeFalse();
    expect(component.error()).toBeNull();
  });

  it('cancel should be a no-op when order is null', () => {
    const fixture = TestBed.createComponent(OrderDetail);
    const component = fixture.componentInstance;
    // order() is null by default, cancel() should return immediately without calling the API.
    const spy = spyOn<any>(component['api'], 'cancel').and.callThrough();
    component.cancel();
    expect(spy).not.toHaveBeenCalled();
  });
});
