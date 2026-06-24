import { Inject, Injectable, signal } from '@angular/core';
import {
  AccountInfo,
  AuthenticationResult,
  InteractionRequiredAuthError,
  IPublicClientApplication,
} from '@azure/msal-browser';
import { MSAL_INSTANCE, apiTokenRequest, loginRequest } from './auth.config';

/**
 * Thin wrapper over MSAL: initialization, interactive login/logout, and silent access-token
 * acquisition for the API (falling back to interactive when MSAL needs user interaction).
 * `account` is a signal so the UI reacts to sign-in/out.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly account = signal<AccountInfo | null>(null);

  constructor(@Inject(MSAL_INSTANCE) private readonly msal: IPublicClientApplication) {}

  /** Called once at startup: initialize MSAL and complete any redirect sign-in. */
  async initialize(): Promise<void> {
    await this.msal.initialize();

    const result = await this.msal.handleRedirectPromise();
    if (result?.account) {
      this.msal.setActiveAccount(result.account);
    } else {
      const existing = this.msal.getActiveAccount() ?? this.msal.getAllAccounts()[0] ?? null;
      if (existing) this.msal.setActiveAccount(existing);
    }
    this.account.set(this.msal.getActiveAccount());
  }

  get isAuthenticated(): boolean {
    return this.account() !== null;
  }

  login(): Promise<void> {
    return this.msal.loginRedirect(loginRequest);
  }

  logout(): Promise<void> {
    return this.msal.logoutRedirect();
  }

  /** Acquire an API access token silently, falling back to a redirect when required. */
  async getApiToken(): Promise<string | null> {
    const account = this.msal.getActiveAccount();
    if (!account) return null;

    try {
      const result: AuthenticationResult = await this.msal.acquireTokenSilent({
        ...apiTokenRequest,
        account,
      });
      return result.accessToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        await this.msal.acquireTokenRedirect(apiTokenRequest);
      }
      return null;
    }
  }
}
