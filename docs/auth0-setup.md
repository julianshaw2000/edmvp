# Auth0 Setup Guide — Tungsten Pilot MVP

## 1. Create Auth0 Account

Go to https://auth0.com and create a free account (or use an existing one).

## 2. Create SPA Application

1. Go to **Applications > Applications > Create Application**
2. Name: `Tungsten Web`
3. Type: **Single Page Application**
4. Click Create
5. Go to **Settings** tab and configure:
   - **Allowed Callback URLs:** `http://localhost:4200, https://auditraks.com`
   - **Allowed Logout URLs:** `http://localhost:4200, https://auditraks.com`
   - **Allowed Web Origins:** `http://localhost:4200, https://auditraks.com`
   - **ID Token Expiration:** 28800 (8 hours = FR-P064)
6. Save Changes
7. Note the **Domain** and **Client ID**

## 3. Create API

1. Go to **Applications > APIs > Create API**
2. Name: `auditraks API`
3. Identifier: `https://api.auditraks.com`
4. Signing Algorithm: RS256
5. Click Create

## 4. Configure the Application

### Angular (`packages/web/src/environments/environment.ts`)

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  auth0: {
    domain: 'YOUR_TENANT.auth0.com',     // ← from step 2
    clientId: 'YOUR_CLIENT_ID',           // ← from step 2
    audience: 'https://api.auditraks.com',     // ← from step 3
  },
};
```

### .NET API (`packages/api/src/Tungsten.Api/appsettings.Development.json`)

Add the Auth0 section:
```json
{
  "Auth0": {
    "Domain": "YOUR_TENANT.auth0.com",
    "Audience": "https://api.auditraks.com"
  }
}
```

## 5. Create Test Users

1. Go to **User Management > Users > Create User**
2. Create three users with email/password:
   - `supplier@tungsten-pilot.com` (will be assigned SUPPLIER role)
   - `buyer@tungsten-pilot.com` (will be assigned BUYER role)
   - `admin@tungsten-pilot.com` (will be assigned PLATFORM_ADMIN role)

3. After creating Auth0 users, add them to the platform database. Update `SeedData.cs` or create them via the Admin Portal once running.

## 6. Disable Refresh Tokens (FR-P064)

1. Go to **Applications > YOUR_APP > Settings > Advanced Settings**
2. Under **Grant Types**, uncheck `Refresh Token`
3. Save

## 7. Running Locally

```bash
# Terminal 1: Start API
cd packages/api/src/Tungsten.Api
dotnet run

# Terminal 2: Start Angular
cd packages/web
ng serve
```

Navigate to http://localhost:4200 — you should see the login page. Click "Sign in" to authenticate via Auth0.
