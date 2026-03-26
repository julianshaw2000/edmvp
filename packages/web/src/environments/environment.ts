export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  msal: {
    clientId: '600ab450-82f0-4200-a6bd-7f59068dbab8',
    authority: 'https://accutracext.ciamlogin.com/accutracext.onmicrosoft.com',
    redirectUri: 'http://localhost:4200/login',
    // CIAM SSPR authority — configure this after creating a password reset user flow in Entra External ID
    resetPasswordAuthority: 'https://accutracext.ciamlogin.com/accutracext.onmicrosoft.com',
    apiClientId: '4de43332-2e6e-4ece-9c05-7582c74024dd',
  },
};
