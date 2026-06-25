import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { AuthService } from './auth.service';

/** Blocks a route unless the user is signed in; triggers an interactive login otherwise. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated) return true;

  void auth.login();
  return false;
};
