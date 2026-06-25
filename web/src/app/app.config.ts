import {
  ApplicationConfig,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  inject,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { MSAL_INSTANCE, createMsalInstance } from './core/auth/auth.config';
import { AuthService } from './core/auth/auth.service';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),

    // Single MSAL instance for the app.
    { provide: MSAL_INSTANCE, useFactory: createMsalInstance },

    // Initialize MSAL and complete any redirect sign-in before the first route renders.
    provideAppInitializer(() => inject(AuthService).initialize()),
  ],
};
