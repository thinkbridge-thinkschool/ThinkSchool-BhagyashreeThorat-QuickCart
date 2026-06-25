import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

/**
 * Attaches the Entra API access token as a Bearer header on calls to the QuickCart API.
 * Requests to other origins pass through untouched.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }

  const auth = inject(AuthService);
  return from(auth.getApiToken()).pipe(
    switchMap((token) => {
      const authorized = token
        ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : req;
      return next(authorized);
    }),
  );
};
