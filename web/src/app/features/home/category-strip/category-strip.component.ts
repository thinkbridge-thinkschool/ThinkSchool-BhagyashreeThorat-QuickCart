import { Component, input, output, computed } from '@angular/core';
import { Category } from '../../../core/models';

/** Maps lowercase category name fragments to an emoji icon. */
const ICONS: [string, string][] = [
  // Produce
  ['fruit', '🍎'], ['vegetable', '🥕'],
  // Grocery
  ['grocery', '🥦'], ['staple', '🌾'],
  // Dairy
  ['dairy', '🥛'], ['breakfast', '🥣'], ['egg', '🥚'],
  // Bakery
  ['bread', '🍞'], ['bakery', '🥐'],
  // Meat & fish
  ['meat', '🍗'], ['fish', '🐟'],
  // Beverages
  ['beverage', '🥤'], ['tea', '🍵'], ['coffee', '☕'],
  // Snacks
  ['snack', '🍿'], ['munchie', '🍟'], ['chip', '🥨'], ['biscuit', '🍪'], ['chocolate', '🍫'],
  // Frozen
  ['frozen', '❄️'],
  // Personal care (before generic 'care')
  ['personal care', '🧴'],
  // Home & cleaning
  ['home', '🏠'], ['cleaning', '🧹'],
  // Baby (before generic 'care')
  ['baby', '👶'],
  // Health & wellness
  ['health', '💊'], ['wellness', '🌿'], ['vitamin', '💊'],
  // Electronics
  ['electronic', '📱'], ['gadget', '🔌'], ['accessorie', '🎧'],
  // Pet
  ['pet', '🐾'],
  // Generic care fallback
  ['care', '🧼'],
  // Spices / masala
  ['masala', '🌶️'], ['spice', '🌶️'],
  // Packaged
  ['packaged', '📦'],
];

export interface CategoryCard {
  category: Category;
  icon: string;
}

function iconFor(name: string): string {
  const lower = name.toLowerCase();
  for (const [key, emoji] of ICONS) {
    if (lower.includes(key)) return emoji;
  }
  return '🛍️';
}

@Component({
  selector: 'app-category-strip',
  templateUrl: './category-strip.component.html',
  styleUrl: './category-strip.component.css',
})
export class CategoryStrip {
  /** Categories fetched by the parent. */
  readonly categories = input.required<Category[]>();

  /** Currently selected category id, or null for "All". */
  readonly selected = input<string | null>(null);

  /** Emits the chosen category id, or null when the user clicks "All". */
  readonly categorySelect = output<string | null>();

  readonly cards = computed<CategoryCard[]>(() =>
    this.categories().map((c) => ({
      category: c,
      icon: iconFor(c.categoryName),
    }))
  );

  select(id: string | null): void {
    this.categorySelect.emit(id);
  }
}
