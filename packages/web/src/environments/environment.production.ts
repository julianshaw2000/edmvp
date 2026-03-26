export const environment = {
  production: true,
  apiUrl: 'https://accutrac-api.onrender.com',
  msal: {
    clientId: 'MSAL_CLIENT_ID',
    authority: 'https://TENANT_SUBDOMAIN.ciamlogin.com/TENANT_SUBDOMAIN.onmicrosoft.com',
    redirectUri: 'https://auditraks.com/login',
    // CIAM SSPR authority — NOT a B2C user flow path. Format: https://<tenant>.ciamlogin.com/<tenant>.onmicrosoft.com?p=<policy>
    // Replace with your actual Entra External ID SSPR/password-reset policy URL.
    resetPasswordAuthority: 'https://TENANT_SUBDOMAIN.ciamlogin.com/TENANT_SUBDOMAIN.onmicrosoft.com/B2C_1_password_reset',
    apiClientId: 'API_CLIENT_ID',
  },
};
