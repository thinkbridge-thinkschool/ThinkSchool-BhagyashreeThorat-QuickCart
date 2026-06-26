import { Component, input, output } from '@angular/core';
import { Category } from '../../core/models';

const IMG_BASE = 'assets/images/categories/';

const CATEGORY_IMAGES: [string, string][] = [
  ['fruit',      'fruits-and-vegetables.svg'],
  ['vegetable',  'fruits-and-vegetables.svg'],
  ['dairy',      'dairy-and-breakfast.svg'],
  ['breakfast',  'dairy-and-breakfast.svg'],
  ['grocery',    'grocery-and-staples.svg'],
  ['staple',     'grocery-and-staples.svg'],
  ['snack',      'snacks-and-munchies.svg'],
  ['munchie',    'snacks-and-munchies.svg'],
  ['beverage',   'beverages.svg'],
  ['frozen',     'frozen-foods.svg'],
  ['personal',   'personal-care.svg'],
  ['home',       'home-and-cleaning.svg'],
  ['cleaning',   'home-and-cleaning.svg'],
  ['health',     'health-and-wellness.svg'],
  ['wellness',   'health-and-wellness.svg'],
  ['electronic', 'electronics.svg'],
  ['pet',        'pet-care.svg'],
  ['bread',      'bread-and-bakery.svg'],
  ['bakery',     'bread-and-bakery.svg'],
];

function imageFor(name: string): string {
  const lower = name.toLowerCase();
  for (const [key, file] of CATEGORY_IMAGES) {
    if (lower.includes(key)) return IMG_BASE + file;
  }
  return IMG_BASE + 'grocery-and-staples.svg';
}

export interface CategoryImageCard {
  category: Category;
  image: string;
}

@Component({
  selector: 'app-category-image-strip',
  imports: [],
  templateUrl: './category-image-strip.component.html',
  styleUrl: './category-image-strip.component.css',
})
export class CategoryImageStrip {
  readonly categories = input.required<Category[]>();
  readonly selected   = input<string | null>(null);
  readonly categorySelect = output<string | null>();

  get cards(): CategoryImageCard[] {
    return this.categories().map(c => ({ category: c, image: imageFor(c.categoryName) }));
  }

  select(id: string | null): void {
    this.categorySelect.emit(id);
  }
}
