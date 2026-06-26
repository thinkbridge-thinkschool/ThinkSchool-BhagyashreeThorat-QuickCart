/**
 * Production environment — Angular is served from the same App Service as the API,
 * so apiBaseUrl uses a root-relative path (no CORS required).
 * auth values are public identifiers from the Entra App Registration; no secrets here.
 */
export const environment = {
  production: true,

  // Root-relative: resolves to the App Service origin at runtime.
  apiBaseUrl: '/api/v1',

  auth: {
    tenantId: 'common',
    clientId: 'f58ac8a6-b34d-43f1-9593-3935cb98281d',
    redirectUri: 'https://app-quickcart-dev-wl2vpuykc6hy6.azurewebsites.net',
    postLogoutRedirectUri: 'https://app-quickcart-dev-wl2vpuykc6hy6.azurewebsites.net',
    apiScope: 'api://f58ac8a6-b34d-43f1-9593-3935cb98281d/access_as_user',
  },
};
