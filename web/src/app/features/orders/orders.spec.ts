import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Orders } from './orders';

describe('Orders', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Orders],
      providers: [provideHttpClient(), provideRouter([])],
    });
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Orders);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should start with loading true and empty orders list', () => {
    const fixture = TestBed.createComponent(Orders);
    const component = fixture.componentInstance;
    expect(component.loading()).toBeTrue();
    expect(component.orders()).toEqual([]);
  });
});
