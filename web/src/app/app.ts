import { Component, effect, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from './core/auth/auth.service';
import { SearchState } from './core/state/search.service';
import { CartState } from './core/state/cart-state.service';

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

  constructor() {
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

  login() { void this.auth.login(); }
  logout() { void this.auth.logout(); }
}
