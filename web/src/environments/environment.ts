/**
 * Frontend configuration. Fill in `clientId` (and keep `apiScope` in sync) with the values from
 * the Entra App Registration described in DESIGN.md / the Day 29 notes. `tenantId` is the known
 * QuickCart tenant. No secrets here — these are all public identifiers.
 */
export const environment = {
  production: false,

  // The QuickCart API. Local dev uses the HTTP endpoint (no dev-cert trust needed). To use
  // HTTPS instead, run the API with `--launch-profile https` and trust the cert, then switch
  // this to 'https://localhost:7284/api/v1'.
  apiBaseUrl: 'http://localhost:5106/api/v1',

  auth: {
    tenantId: '7e394fc8-4b86-4cfe-810e-43f86f8bec47',
    clientId: 'f58ac8a6-b34d-43f1-9593-3935cb98281d',
    redirectUri: 'http://localhost:4200',
    postLogoutRedirectUri: 'http://localhost:4200',
    // Delegated scope exposed by the API registration (api://<client-id>/access_as_user).
    apiScope: 'api://f58ac8a6-b34d-43f1-9593-3935cb98281d/access_as_user',
  },
};
