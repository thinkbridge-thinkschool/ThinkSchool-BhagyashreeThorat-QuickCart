import { TestBed, ComponentFixture } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { CategoryStrip } from './category-strip.component';
import { Category } from '../../../core/models';

const CATS: Category[] = [
  { categoryId: 'c1', categoryName: 'Grocery', description: null },
  { categoryId: 'c2', categoryName: 'Beverages', description: null },
];

describe('CategoryStrip', () => {
  let fixture: ComponentFixture<CategoryStrip>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [CategoryStrip] });
    fixture = TestBed.createComponent(CategoryStrip);
    fixture.componentRef.setInput('categories', CATS);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render one card per category plus the All button', () => {
    const cards = fixture.debugElement.queryAll(By.css('.cat-card'));
    expect(cards.length).toBe(CATS.length + 1); // All + each category
  });

  it('should mark "All" as active when selected is null', () => {
    fixture.componentRef.setInput('selected', null);
    fixture.detectChanges();
    const allBtn = fixture.debugElement.queryAll(By.css('.cat-card'))[0];
    expect(allBtn.classes['active']).toBeTrue();
  });

  it('should mark a category card active when its id is selected', () => {
    fixture.componentRef.setInput('selected', 'c1');
    fixture.detectChanges();
    const cards = fixture.debugElement.queryAll(By.css('.cat-card'));
    expect(cards[1].classes['active']).toBeTrue();
    expect(cards[2].classes['active']).toBeFalsy();
  });

  it('should emit null when All is clicked', () => {
    let emitted: string | null | undefined = 'sentinel';
    fixture.componentInstance.categorySelect.subscribe((v) => (emitted = v));
    const allBtn = fixture.debugElement.queryAll(By.css('.cat-card'))[0];
    allBtn.triggerEventHandler('click', null);
    expect(emitted).toBeNull();
  });

  it('should emit categoryId when a category card is clicked', () => {
    let emitted: string | null | undefined;
    fixture.componentInstance.categorySelect.subscribe((v) => (emitted = v));
    const secondCard = fixture.debugElement.queryAll(By.css('.cat-card'))[1];
    secondCard.triggerEventHandler('click', null);
    expect(emitted).toBe('c1');
  });
});
