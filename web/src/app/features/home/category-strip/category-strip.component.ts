import { Component, input, output, computed } from '@angular/core';
import { Category } from '../../../core/models';

/** Maps lowercase category name fragments to an emoji icon. */
const ICONS: [string, string][] = [
  ['grocery', '🥦'], ['fruit', '🍎'], ['vegetable', '🥕'],
  ['dairy', '🥛'], ['bread', '🍞'], ['egg', '🥚'],
  ['meat', '🍗'], ['fish', '🐟'],
  ['beverage', '🥤'], ['tea', '🍵'], ['coffee', '☕'],
  ['snack', '🍿'], ['chip', '🥨'], ['biscuit', '🍪'],
  ['personal care', '🧴'], ['care', '🧼'],
  ['electronic', '📱'], ['gadget', '🔌'],
  ['frozen', '❄️'], ['masala', '🌶️'], ['breakfast', '🥣'],
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
