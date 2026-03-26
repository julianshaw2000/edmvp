export const environment = {
  production: true,
  apiUrl: 'https://accutrac-api.onrender.com',
  msal: {
    clientId: '600ab450-82f0-4200-a6bd-7f59068dbab8',
    authority: 'https://accutracext.ciamlogin.com/accutracext.onmicrosoft.com',
    redirectUri: 'https://auditraks.com/login',
    // CIAM SSPR authority — configure after creating a password reset user flow in Entra External ID
    resetPasswordAuthority: 'https://accutracext.ciamlogin.com/accutracext.onmicrosoft.com',
    apiClientId: '4de43332-2e6e-4ece-9c05-7582c74024dd',
  },
};
