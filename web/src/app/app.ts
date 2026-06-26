import { Component, effect, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from './core/auth/auth.service';
import { SearchState } from './core/state/search.service';
import { CartState } from './core/state/cart-state.service';

/** Cities where delivery is active. All others are shown greyed-out and non-clickable. */
const ACTIVE_CITIES = new Set(['Pune']);

const CITIES = [
  'Pune',
  'Mumbai', 'Delhi', 'Bangalore',
  'Chennai', 'Hyderabad', 'Kolkata', 'Ahmedabad',
];

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly cartState = inject(CartState);
  private readonly search = inject(SearchState);
  private readonly router = inject(Router);

  protected term = '';

  // ── Location picker ──────────────────────────────────────────────────
  protected readonly cities = CITIES;
  protected readonly isActive = (city: string) => ACTIVE_CITIES.has(city);
  protected readonly selectedCity = signal<string | null>(null);
  protected readonly locationOpen = signal(false);

  constructor() {
    // Sync the local term with SearchState so the input always reflects the real state.
    this.term = this.search.term();

    // Once the user signs in, load the initial cart count for the badge.
    effect(() => {
      if (this.auth.account()) {
        this.cartState.refresh();
      }
    });
  }

  protected displayName(): string {
    const a = this.auth.account();
    return a?.name ?? a?.username ?? 'Account';
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    if (this.auth.isAuthenticated) {
      void this.router.navigate(['/products']);
    }
  }

  protected selectCity(city: string): void {
    this.selectedCity.set(city);
    this.locationOpen.set(false);
  }

  login()  { void this.auth.login(); }
  logout() { void this.auth.logout(); }
}
