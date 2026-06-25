import { Injectable, signal } from '@angular/core';

/**
 * Shared search term, written by the navbar search box and read by the Products grid.
 * A tiny signal-backed service keeps the two components decoupled while giving instant,
 * type-to-filter behaviour without round-tripping the API on every keystroke.
 */
@Injectable({ providedIn: 'root' })
export class SearchState {
  /** Current free-text product search term (empty = no filter). */
  readonly term = signal('');

  set(value: string): void {
    this.term.set(value);
  }

  clear(): void {
    this.term.set('');
  }
}
