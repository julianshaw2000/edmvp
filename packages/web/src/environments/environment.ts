export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  msal: {
    clientId: 'MSAL_CLIENT_ID',
    authority: 'https://TENANT_SUBDOMAIN.ciamlogin.com/TENANT_SUBDOMAIN.onmicrosoft.com',
    redirectUri: 'http://localhost:4200/login',
    resetPasswordAuthority: 'https://TENANT_SUBDOMAIN.ciamlogin.com/TENANT_SUBDOMAIN.onmicrosoft.com/B2C_1_password_reset',
    apiClientId: 'API_CLIENT_ID',
  },
};
