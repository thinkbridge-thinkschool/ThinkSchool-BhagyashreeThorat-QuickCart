import { InjectionToken } from '@angular/core';
import { IPublicClientApplication, PublicClientApplication } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';

/** DI token carrying the singleton MSAL application instance. */
export const MSAL_INSTANCE = new InjectionToken<IPublicClientApplication>('MSAL_INSTANCE');

/** Builds the MSAL PublicClientApplication from environment config. */
export function createMsalInstance(): IPublicClientApplication {
  return new PublicClientApplication({
    auth: {
      clientId: environment.auth.clientId,
      authority: `https://login.microsoftonline.com/${environment.auth.tenantId}`,
      redirectUri: environment.auth.redirectUri,
      postLogoutRedirectUri: environment.auth.postLogoutRedirectUri,
    },
    cache: {
      // sessionStorage keeps tokens scoped to the tab and out of long-lived localStorage.
      cacheLocation: 'sessionStorage',
    },
  });
}

/** Scopes requested when acquiring an access token for the QuickCart API. */
export const apiTokenRequest = { scopes: [environment.auth.apiScope] };

/** Scopes requested at interactive login (API access + basic profile). */
export const loginRequest = { scopes: [environment.auth.apiScope, 'openid', 'profile', 'email'] };
