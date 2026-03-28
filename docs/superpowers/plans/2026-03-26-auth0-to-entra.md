# Auth0 → Microsoft Entra External ID Migration Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Auth0 with Microsoft Entra External ID across the Tungsten MVP — frontend, backend, and database — adding Google social login and self-service password reset.

**Architecture:** Frontend swaps `@auth0/auth0-angular` for `@azure/msal-angular`. `MsalInterceptor` attaches tokens; `MsalGuard` protects routes; `MsalBroadcastService` coordinates redirect lifecycle in `app.ts`. Backend replaces manual `AddJwtBearer` (Auth0) with `AddMicrosoftIdentityWebApiAuthentication` from `Microsoft.Identity.Web`. The `auth0_sub` DB column renames to `entra_oid` via an EF Core migration; the C# property `Auth0Sub` renames to `EntraOid` on `UserEntity` and `ICurrentUserService`, with a global find-replace cascading through all callers.

**Tech Stack:** Angular 21 (standalone), @azure/msal-angular v3, @azure/msal-browser v3, .NET 10, Microsoft.Identity.Web v3, EF Core 10, PostgreSQL (Neon)

---

## File Map

### Frontend — `packages/web/`
| Action | File | What changes |
|--------|------|--------------|
| Modify | `package.json` | Remove `@auth0/auth0-angular`, add `@azure/msal-angular` + `@azure/msal-browser` |
| Modify | `src/environments/environment.ts` | Replace `auth0` block with `msal` block |
| Modify | `src/environments/environment.production.ts` | Same as above |
| Replace | `src/app/app.config.ts` | Remove `provideAuth0`, add MSAL instance/guard/interceptor providers |
| Replace | `src/app/core/auth/auth.service.ts` | Auth0Service → MsalService, keep same public API (login/logout/loadProfile/profile/role) |
| Replace | `src/app/core/auth/auth.guard.ts` | Auth0 isLoading/isAuthenticated → MsalGuard delegation |
| Delete | `src/app/core/auth/auth.interceptor.ts` | Replaced entirely by class-based `MsalInterceptor` |
| Modify | `src/app/app.ts` | Add `handleRedirectObservable()` + active account setup in OnInit |
| Modify | `src/app/features/auth/login.component.ts` | Remove direct `Auth0Service` inject; use `MsalBroadcastService` to detect auth completion |
| Modify | `src/app/app.routes.ts` | Add `/login-failed` route |
| Create | `src/app/features/auth/login-failed.component.ts` | Simple error page for MSAL login failures |

### Backend — `packages/api/`
| Action | File | What changes |
|--------|------|--------------|
| Modify | `src/Tungsten.Api/Tungsten.Api.csproj` | Add `Microsoft.Identity.Web` package |
| Modify | `src/Tungsten.Api/appsettings.json` | Replace `Auth0` block with `AzureAd` block |
| Modify | `src/Tungsten.Api/appsettings.Development.json` | Same |
| Modify | `src/Tungsten.Api/Program.cs` | Replace manual `AddJwtBearer`; update `/api/me` for OID + Google pending_activation |
| Modify | `src/Tungsten.Api/Common/Auth/CurrentUserService.cs` | Rename `Auth0Sub` → `EntraOid`; change claim type from `NameIdentifier` to OID |
| Modify | `src/Tungsten.Api/Common/Auth/ApiKeyMiddleware.cs` | Inject OID claim type instead of `NameIdentifier` |
| Modify | `src/Tungsten.Api/Common/Auth/RoleAuthorizationHandler.cs` | `Auth0Sub` → `EntraOid` |
| Modify | `src/Tungsten.Api/Features/Auth/GetMe.cs` | `Auth0Sub` → `EntraOid` |
| Modify | `src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs` | Rename property `Auth0Sub` → `EntraOid` |
| Modify | `src/Tungsten.Api/Infrastructure/Persistence/Configurations/UserConfiguration.cs` | Update property mapping + `HasColumnName("entra_oid")` |
| Mass rename (src) | All `*.cs` in `src/` | `\.Auth0Sub` → `.EntraOid` in ~15 feature files |
| Mass rename (src) | `Features/Signup/StripeWebhookHandler.cs`, `Features/Platform/CreateTenant.cs` | `Auth0Sub = $"pending|` → `EntraOid = $"pending|` |
| Create | `src/Tungsten.Api/Migrations/<timestamp>_RenameAuth0SubToEntraOid.cs` | EF Core migration for column rename |
| Mass rename (tests) | All `*.cs` in `tests/` | `\.Auth0Sub` → `.EntraOid` |

---

## Chunk 1: Frontend — MSAL Migration

### Task 1: Install MSAL packages

**Files:**
- Modify: `packages/web/package.json`

- [ ] **Step 1: Run package swap**

```bash
cd packages/web
npm uninstall @auth0/auth0-angular
npm install @azure/msal-angular@^3 @azure/msal-browser@^3
```

- [ ] **Step 2: Verify package.json**

```bash
grep -E "msal|auth0" packages/web/package.json
```

Expected: two msal lines, zero auth0 lines.

- [ ] **Step 3: Commit**

```bash
git add packages/web/package.json packages/web/package-lock.json
git commit -m "chore: swap @auth0/auth0-angular for @azure/msal-angular + msal-browser"
```

---

### Task 2: Update environment files

**Files:**
- Modify: `packages/web/src/environments/environment.ts`
- Modify: `packages/web/src/environments/environment.production.ts`

- [ ] **Step 1: Replace environment.ts**

Full file content:
```typescript
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
```

> Note: all `MSAL_*` and `API_CLIENT_ID` values come from Render environment variables at build time. Replace placeholders with the actual values from the Render dashboard before deploying.

- [ ] **Step 2: Replace environment.production.ts**

Full file content:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://accutrac-api.onrender.com',
  msal: {
    clientId: 'MSAL_CLIENT_ID',
    authority: 'https://TENANT_SUBDOMAIN.ciamlogin.com/TENANT_SUBDOMAIN.onmicrosoft.com',
    redirectUri: 'https://auditraks.com/login',
    resetPasswordAuthority: 'https://TENANT_SUBDOMAIN.ciamlogin.com/TENANT_SUBDOMAIN.onmicrosoft.com/B2C_1_password_reset',
    apiClientId: 'API_CLIENT_ID',
  },
};
```

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/environments/
git commit -m "feat: update environment files for MSAL config shape"
```

---

### Task 3: Replace app.config.ts

**Files:**
- Replace: `packages/web/src/app/app.config.ts`

- [ ] **Step 1: Write new app.config.ts**

Full file content:
```typescript
import { ApplicationConfig, provideZoneChangeDetection, isDevMode } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors, HTTP_INTERCEPTORS } from '@angular/common/http';
import {
  MsalService,
  MsalGuard,
  MsalBroadcastService,
  MsalInterceptor,
  MSAL_INSTANCE,
  MSAL_GUARD_CONFIG,
  MSAL_INTERCEPTOR_CONFIG,
  MsalGuardConfiguration,
  MsalInterceptorConfiguration,
} from '@azure/msal-angular';
import {
  PublicClientApplication,
  InteractionType,
  BrowserCacheLocation,
  LogLevel,
} from '@azure/msal-browser';
import { routes } from './app.routes';
import { errorInterceptor } from './core/http/error.interceptor';
import { environment } from '../environments/environment';
import { provideServiceWorker } from '@angular/service-worker';

export function msalInstanceFactory(): PublicClientApplication {
  return new PublicClientApplication({
    auth: {
      clientId: environment.msal.clientId,
      authority: environment.msal.authority,
      redirectUri: environment.msal.redirectUri,
      knownAuthorities: [new URL(environment.msal.authority).hostname],
    },
    cache: {
      cacheLocation: BrowserCacheLocation.SessionStorage,
      storeAuthStateInCookie: false,
    },
    system: {
      loggerOptions: {
        loggerCallback: () => {},
        logLevel: LogLevel.Warning,
        piiLoggingEnabled: false,
      },
    },
  });
}

export function msalGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: {
      scopes: [`api://${environment.msal.apiClientId}/.default`],
    },
    loginFailedRoute: '/login-failed',
  };
}

export function msalInterceptorConfigFactory(): MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, Array<string>>();
  protectedResourceMap.set(
    environment.apiUrl,
    [`api://${environment.msal.apiClientId}/.default`],
  );
  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap,
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([errorInterceptor])),
    { provide: MSAL_INSTANCE, useFactory: msalInstanceFactory },
    { provide: MSAL_GUARD_CONFIG, useFactory: msalGuardConfigFactory },
    { provide: MSAL_INTERCEPTOR_CONFIG, useFactory: msalInterceptorConfigFactory },
    MsalService,
    MsalGuard,
    MsalBroadcastService,
    { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true },
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
```

> Key points: `authInterceptor` is removed — `MsalInterceptor` handles bearer tokens. `errorInterceptor` stays. Cache is `SessionStorage` not `localStorage`. `piiLoggingEnabled: false`.

- [ ] **Step 2: Commit**

```bash
git add packages/web/src/app/app.config.ts
git commit -m "feat: replace Auth0 providers with MSAL in app.config.ts"
```

---

### Task 4: Replace auth.service.ts

**Files:**
- Replace: `packages/web/src/app/core/auth/auth.service.ts`

The public API (`login()`, `logout()`, `loadProfile()`, `profile`, `role`, `profileError`) is preserved so callers don't need changes.

- [ ] **Step 1: Write new auth.service.ts**

Full file content:
```typescript
import { Injectable, inject, signal, computed } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { HttpClient } from '@angular/common/http';
import { catchError, of } from 'rxjs';
import { API_URL } from '../http/api-url.token';
import { environment } from '../../../environments/environment';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  role: 'SUPPLIER' | 'BUYER' | 'PLATFORM_ADMIN' | 'TENANT_ADMIN';
  tenantId: string;
  tenantName: string;
  tenantStatus: string;
  trialEndsAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private msal = inject(MsalService);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  private _profile = signal<UserProfile | null>(null);
  private _profileLoading = signal(false);
  private _profileError = signal<string | null>(null);
  readonly profile = this._profile.asReadonly();
  readonly profileLoading = this._profileLoading.asReadonly();
  readonly profileError = this._profileError.asReadonly();
  readonly role = computed(() => this._profile()?.role ?? null);

  isLoggedIn(): boolean {
    return this.msal.instance.getAllAccounts().length > 0;
  }

  login() {
    this.msal.loginRedirect({
      scopes: [`api://${environment.msal.apiClientId}/.default`],
    });
  }

  logout() {
    this.msal.logoutRedirect({
      postLogoutRedirectUri: window.location.origin,
    });
  }

  resetPassword() {
    this.msal.loginRedirect({
      authority: environment.msal.resetPasswordAuthority,
      scopes: [],
    });
  }

  loadProfile(): Promise<UserProfile | null> {
    this._profileLoading.set(true);
    this._profileError.set(null);
    return new Promise(resolve => {
      this.http.get<UserProfile>(`${this.apiUrl}/api/me`).pipe(
        catchError((err) => {
          if (err?.status === 403) {
            this._profileError.set('No account found. Contact your administrator to get access.');
          } else {
            this._profileError.set('Failed to load profile. The server may be starting up — please wait a moment.');
          }
          return of(null);
        })
      ).subscribe(profile => {
        this._profile.set(profile);
        this._profileLoading.set(false);
        resolve(profile);
      });
    });
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add packages/web/src/app/core/auth/auth.service.ts
git commit -m "feat: replace Auth0Service with MsalService in auth.service.ts"
```

---

### Task 5: Replace auth.guard.ts and delete auth.interceptor.ts

**Files:**
- Replace: `packages/web/src/app/core/auth/auth.guard.ts`
- Delete: `packages/web/src/app/core/auth/auth.interceptor.ts`

- [ ] **Step 1: Write new auth.guard.ts**

Full file content:
```typescript
import { inject } from '@angular/core';
import { CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';

export const authGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot,
) => inject(MsalGuard).canActivate(route, state);
```

Routes continue to use `canActivate: [authGuard]` without modification.

- [ ] **Step 2: Delete auth.interceptor.ts**

```bash
rm packages/web/src/app/core/auth/auth.interceptor.ts
```

`MsalInterceptor` (registered in `app.config.ts`) replaces it entirely.

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/core/auth/auth.guard.ts
git rm packages/web/src/app/core/auth/auth.interceptor.ts
git commit -m "feat: replace Auth0 guard with MsalGuard wrapper; delete auth.interceptor (MsalInterceptor replaces it)"
```

---

### Task 6: Update app.ts — handleRedirectObservable

**Files:**
- Modify: `packages/web/src/app/app.ts`

MSAL requires `handleRedirectObservable()` to be called once on app startup to process the redirect response.

- [ ] **Step 1: Write new app.ts**

Full file content:
```typescript
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { EventMessage, EventType, InteractionStatus } from '@azure/msal-browser';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class AppComponent implements OnInit, OnDestroy {
  private msal = inject(MsalService);
  private broadcastService = inject(MsalBroadcastService);
  private readonly destroying$ = new Subject<void>();

  ngOnInit() {
    // Required: process redirect response before any other MSAL calls
    this.msal.handleRedirectObservable().subscribe();

    // Set active account once MSAL finishes any in-progress interaction
    this.broadcastService.inProgress$
      .pipe(
        filter(status => status === InteractionStatus.None),
        takeUntil(this.destroying$),
      )
      .subscribe(() => {
        const accounts = this.msal.instance.getAllAccounts();
        if (accounts.length > 0 && !this.msal.instance.getActiveAccount()) {
          this.msal.instance.setActiveAccount(accounts[0]);
        }
      });

    // Set active account immediately on successful login
    this.broadcastService.msalSubject$
      .pipe(
        filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS),
        takeUntil(this.destroying$),
      )
      .subscribe((result: EventMessage) => {
        const payload = result.payload as { account?: { username: string } };
        const accounts = this.msal.instance.getAllAccounts();
        if (accounts.length > 0) {
          this.msal.instance.setActiveAccount(accounts[0]);
        }
      });
  }

  ngOnDestroy() {
    this.destroying$.next();
    this.destroying$.complete();
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add packages/web/src/app/app.ts
git commit -m "feat: add MSAL redirect handling and active account setup to AppComponent"
```

---

### Task 7: Update login.component.ts

**Files:**
- Modify: `packages/web/src/app/features/auth/login.component.ts`

Remove direct `Auth0Service` inject. Use `MsalBroadcastService.inProgress$` to detect when MSAL finishes, then load profile.

- [ ] **Step 1: Write updated login.component.ts**

Full file content:
```typescript
import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { MsalBroadcastService } from '@azure/msal-angular';
import { InteractionStatus } from '@azure/msal-browser';
import { AuthService } from '../../core/auth/auth.service';
import { API_URL } from '../../core/http/api-url.token';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50 px-6">
      <div class="w-full max-w-sm text-center">
        <div class="flex items-center gap-2 justify-center mb-8">
          <img src="assets/auditraks-logo.png" alt="auditraks" class="h-10" />
        </div>

        @if (errorMessage) {
          <div class="bg-white rounded-2xl shadow-sm border border-slate-200 p-8">
            <div class="mb-5 bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
              <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="text-sm text-rose-700 text-left">{{ errorMessage }}</p>
            </div>
            <button
              (click)="auth.login()"
              class="w-full bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 transition-all mb-4"
            >
              Try again
            </button>
            <a routerLink="/signup" class="text-sm text-indigo-600 hover:underline">
              Don't have an account? Start a free trial
            </a>
          </div>
        } @else {
          <div class="flex flex-col items-center py-6">
            <div class="w-10 h-10 border-3 border-indigo-600 border-t-transparent rounded-full animate-spin mb-4"></div>
            <p class="text-sm text-slate-500">{{ loadingMessage }}</p>
          </div>
        }
      </div>
    </div>
  `,
})
export class LoginComponent implements OnInit, OnDestroy {
  protected auth = inject(AuthService);
  private broadcastService = inject(MsalBroadcastService);
  private router = inject(Router);
  private apiUrl = inject(API_URL);
  loadingMessage = 'Checking authentication...';
  errorMessage = '';
  private readonly destroying$ = new Subject<void>();

  ngOnInit() {
    this.broadcastService.inProgress$
      .pipe(
        filter(status => status === InteractionStatus.None),
        takeUntil(this.destroying$),
      )
      .subscribe(async () => {
        if (this.auth.isLoggedIn()) {
          this.loadingMessage = 'Loading your profile...';
          const profile = await this.auth.loadProfile();
          if (profile) {
            this.navigateByRole(profile.role);
          } else if (this.auth.profileError()?.startsWith('No account found')) {
            this.errorMessage = 'No account found. Contact your administrator to get access.';
          } else {
            this.loadingMessage = 'Server is starting up, please wait...';
            await this.waitForBackend();
            const retry = await this.auth.loadProfile();
            if (retry) {
              this.navigateByRole(retry.role);
            } else {
              this.errorMessage = this.auth.profileError() || 'Unable to load your profile. Please try again.';
            }
          }
        } else {
          this.loadingMessage = 'Redirecting to sign in...';
          this.auth.login();
        }
      });
  }

  ngOnDestroy() {
    this.destroying$.next();
    this.destroying$.complete();
  }

  private navigateByRole(role: string) {
    this.loadingMessage = 'Redirecting...';
    if (role === 'SUPPLIER') this.router.navigate(['/supplier']);
    else if (role === 'BUYER') this.router.navigate(['/buyer']);
    else if (role === 'PLATFORM_ADMIN' || role === 'TENANT_ADMIN') this.router.navigate(['/admin']);
    else this.router.navigate(['/supplier']);
  }

  private async waitForBackend(): Promise<void> {
    const maxAttempts = 15;
    for (let i = 0; i < maxAttempts; i++) {
      try {
        const res = await fetch(`${this.apiUrl}/health`);
        if (res.ok) return;
      } catch { /* server not up yet */ }
      this.loadingMessage = `Server is starting up, please wait... (${i + 1}/${maxAttempts})`;
      await new Promise(r => setTimeout(r, 2000));
    }
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add packages/web/src/app/features/auth/login.component.ts
git commit -m "feat: update LoginComponent to use MsalBroadcastService instead of Auth0"
```

---

### Task 8: Update app.routes.ts + create LoginFailedComponent

**Files:**
- Modify: `packages/web/src/app/app.routes.ts`
- Create: `packages/web/src/app/features/auth/login-failed.component.ts`

- [ ] **Step 1: Create login-failed.component.ts**

```typescript
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login-failed',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50 px-6">
      <div class="w-full max-w-sm text-center">
        <h1 class="text-2xl font-semibold text-slate-800 mb-3">Sign-in failed</h1>
        <p class="text-slate-500 mb-6">Something went wrong during sign-in. Please try again.</p>
        <a routerLink="/login" class="bg-indigo-600 text-white py-3 px-6 rounded-xl font-medium hover:bg-indigo-700 transition-all">
          Try again
        </a>
      </div>
    </div>
  `,
})
export class LoginFailedComponent {}
```

- [ ] **Step 2: Add /login-failed route to app.routes.ts**

In `app.routes.ts`, add after the `/login` route:
```typescript
{
  path: 'login-failed',
  loadComponent: () =>
    import('./features/auth/login-failed.component').then(m => m.LoginFailedComponent),
},
```

- [ ] **Step 3: Build check**

```bash
cd packages/web && ng build 2>&1 | tail -20
```

Expected: no errors. If auth0 import errors appear, search for any missed references:
```bash
grep -r "auth0" packages/web/src --include="*.ts"
```

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/app.routes.ts packages/web/src/app/features/auth/login-failed.component.ts
git commit -m "feat: add /login-failed route and LoginFailedComponent for MSAL error handling"
```

---

## Chunk 2: Backend — NuGet + JWT Config

### Task 9: Add Microsoft.Identity.Web NuGet package

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Tungsten.Api.csproj`

- [ ] **Step 1: Add package**

```bash
cd packages/api/src/Tungsten.Api
dotnet add package Microsoft.Identity.Web --version 3.*
```

- [ ] **Step 2: Remove old JWT package if standalone**

The `.csproj` currently has `Microsoft.AspNetCore.Authentication.JwtBearer`. `Microsoft.Identity.Web` brings its own JWT bearer — keep both for now; the manual `AddJwtBearer` call will be replaced in Task 11. After Task 11, you may remove `Microsoft.AspNetCore.Authentication.JwtBearer` if it's no longer directly referenced.

- [ ] **Step 3: Verify build**

```bash
dotnet build packages/api/src/Tungsten.Api
```

Expected: success (Microsoft.Identity.Web package restores).

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Tungsten.Api.csproj
git commit -m "chore: add Microsoft.Identity.Web NuGet package"
```

---

### Task 10: Update appsettings files

**Files:**
- Modify: `packages/api/src/Tungsten.Api/appsettings.json`
- Modify: `packages/api/src/Tungsten.Api/appsettings.Development.json`

- [ ] **Step 1: Replace Auth0 block in appsettings.json**

Replace:
```json
"Auth0": {
  "Domain": "",
  "Audience": ""
},
```

With:
```json
"AzureAd": {
  "Instance": "https://<TENANT_SUBDOMAIN>.ciamlogin.com/",
  "TenantId": "<TENANT_ID>",
  "ClientId": "<API_CLIENT_ID>",
  "Audience": "api://<API_CLIENT_ID>"
},
```

> Do NOT commit real values. These are placeholder strings — actual values come from Render environment variables.

- [ ] **Step 2: Replace Auth0 block in appsettings.Development.json**

Replace:
```json
"Auth0": {
  "Domain": "dev-htzakhlu.us.auth0.com",
  "Audience": "https://api.accutrac.org"
},
```

With:
```json
"AzureAd": {
  "Instance": "https://<DEV_TENANT_SUBDOMAIN>.ciamlogin.com/",
  "TenantId": "<DEV_TENANT_ID>",
  "ClientId": "<DEV_API_CLIENT_ID>",
  "Audience": "api://<DEV_API_CLIENT_ID>"
},
```

> Fill in the actual dev tenant values from your Entra External ID tenant.

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/appsettings.json packages/api/src/Tungsten.Api/appsettings.Development.json
git commit -m "feat: replace Auth0 config block with AzureAd block in appsettings"
```

---

### Task 11: Replace Auth0 JWT setup in Program.cs

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Add using**

At the top of `Program.cs`, add:
```csharp
using Microsoft.Identity.Web;
```

Remove (if present after the refactor):
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
```

- [ ] **Step 2: Replace the Auth0 JWT block**

Replace this entire block (lines 53–86 in the current file):
```csharp
// Auth0
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrEmpty(auth0Domain))
        {
            options.Authority = $"https://{auth0Domain}/";
            options.Audience = auth0Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://{auth0Domain}/",
                ValidateAudience = true,
                ValidAudience = auth0Audience,
                ValidateLifetime = true,
            };
        }
        else
        {
            // Dev mode: no Auth0 configured — allow anonymous for testing
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.NoResult();
                    return Task.CompletedTask;
                }
            };
        }
    });
```

With:
```csharp
// Entra External ID
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");
```

- [ ] **Step 3: Update Sentry user context comment in Program.cs**

Find the comment:
```csharp
// Sentry user context (Auth0 sub only — no PII)
```
Replace with:
```csharp
// Sentry user context (Entra OID only — no PII)
```

Also update the claim lookup in that same block from:
```csharp
var sub = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
if (sub is not null)
{
    SentrySdk.ConfigureScope(scope =>
    {
        scope.User = new Sentry.SentryUser { Id = sub };
    });
}
```
To:
```csharp
var oid = context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
    ?? context.User.FindFirst("oid")?.Value;
if (oid is not null)
{
    SentrySdk.ConfigureScope(scope =>
    {
        scope.User = new Sentry.SentryUser { Id = oid };
    });
}
```

- [ ] **Step 4: Build check**

```bash
cd packages/api && dotnet build
```

Expected: success. If `JwtBearerDefaults` is now unused, remove `Microsoft.AspNetCore.Authentication.JwtBearer` from the csproj.

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Program.cs packages/api/src/Tungsten.Api/Tungsten.Api.csproj
git commit -m "feat: replace Auth0 JWT bearer with AddMicrosoftIdentityWebApiAuthentication"
```

---

## Chunk 3: Backend — Identity Rename + Migration

### Task 12: Rename UserEntity.Auth0Sub → EntraOid + update EF config

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs`
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/UserConfiguration.cs`

- [ ] **Step 1: Update UserEntity.cs**

Replace:
```csharp
public required string Auth0Sub { get; set; }
```
With:
```csharp
public required string EntraOid { get; set; }
```

- [ ] **Step 2: Update UserConfiguration.cs**

Replace this block:
```csharp
builder.Property(u => u.Auth0Sub)
    .IsRequired()
    .HasMaxLength(200);

builder.HasIndex(u => u.Auth0Sub)
    .IsUnique();
```

With:
```csharp
builder.Property(u => u.EntraOid)
    .HasColumnName("entra_oid")
    .IsRequired()
    .HasMaxLength(200);

builder.HasIndex(u => u.EntraOid)
    .IsUnique();
```

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs
git add packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/UserConfiguration.cs
git commit -m "feat: rename UserEntity.Auth0Sub to EntraOid with HasColumnName(entra_oid)"
```

---

### Task 13: Update ICurrentUserService — rename property + change claim lookup

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs`

- [ ] **Step 1: Write a failing test first**

In `packages/api/tests/Tungsten.Api.Tests/Common/Auth/CurrentUserServiceTests.cs`, add a test that the `EntraOid` property reads the OID claim:

```csharp
[Fact]
public void EntraOid_ReadsOidClaim_WhenPresent()
{
    var oid = Guid.NewGuid().ToString();
    var claims = new[]
    {
        new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", oid),
    };
    var identity = new ClaimsIdentity(claims, "test");
    var principal = new ClaimsPrincipal(identity);

    var httpContext = new DefaultHttpContext { User = principal };
    var accessor = Substitute.For<IHttpContextAccessor>();
    accessor.HttpContext.Returns(httpContext);

    var db = new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    var svc = new CurrentUserService(accessor, db);
    svc.EntraOid.Should().Be(oid);
}
```

- [ ] **Step 2: Run test to confirm it fails**

```bash
cd packages/api && dotnet test --filter "EntraOid_ReadsOidClaim"
```

Expected: FAIL — `EntraOid` property does not exist yet.

- [ ] **Step 3: Update CurrentUserService.cs**

Full file content:
```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public interface ICurrentUserService
{
    string EntraOid { get; }
    Task<Guid> GetUserIdAsync(CancellationToken ct);
    Task<Guid> GetTenantIdAsync(CancellationToken ct);
    Task<string> GetTenantStatusAsync(CancellationToken ct);
    Task<string> GetRoleAsync(CancellationToken ct);
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db) : ICurrentUserService
{
    private Guid? _userId;
    private Guid? _tenantId;
    private string? _role;
    private string? _tenantStatus;

    public string EntraOid =>
        httpContextAccessor.HttpContext?.User
            .FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
        ?? throw new UnauthorizedAccessException("No authenticated user");

    public async Task<Guid> GetUserIdAsync(CancellationToken ct)
    {
        if (_userId.HasValue) return _userId.Value;
        await ResolveUserAsync(ct);
        return _userId!.Value;
    }

    public async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        if (_tenantId.HasValue) return _tenantId.Value;
        await ResolveUserAsync(ct);
        return _tenantId!.Value;
    }

    public async Task<string> GetTenantStatusAsync(CancellationToken ct)
    {
        if (_tenantStatus is not null) return _tenantStatus;
        await ResolveUserAsync(ct);
        return _tenantStatus!;
    }

    public async Task<string> GetRoleAsync(CancellationToken ct)
    {
        if (_role is not null) return _role;
        await ResolveUserAsync(ct);
        return _role!;
    }

    private async Task ResolveUserAsync(CancellationToken ct)
    {
        var oid = EntraOid;
        var user = await db.Users.AsNoTracking()
            .Where(u => u.EntraOid == oid && u.IsActive)
            .Join(db.Tenants, u => u.TenantId, t => t.Id,
                (u, t) => new { u.Id, u.TenantId, u.Role, TenantStatus = t.Status })
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("User not found");

        _userId = user.Id;
        _tenantId = user.TenantId;
        _role = user.Role;
        _tenantStatus = user.TenantStatus;
    }
}
```

- [ ] **Step 4: Run test to confirm it passes**

```bash
cd packages/api && dotnet test --filter "EntraOid_ReadsOidClaim"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs
git add packages/api/tests/Tungsten.Api.Tests/Common/Auth/CurrentUserServiceTests.cs
git commit -m "feat: rename ICurrentUserService.Auth0Sub to EntraOid; read oid claim instead of NameIdentifier"
```

---

### Task 14: Update ApiKeyMiddleware — inject OID claim

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/ApiKeyMiddleware.cs`

API keys impersonate the creator user by injecting claims. `CurrentUserService.EntraOid` now reads the OID claim type, so the middleware must inject that claim type.

- [ ] **Step 1: Update the claims array in ApiKeyMiddleware.cs**

Replace:
```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, key.CreatedBy.Auth0Sub),
    new Claim("api_key_id", key.Id.ToString()),
};
```

With:
```csharp
var claims = new[]
{
    new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", key.CreatedBy.EntraOid),
    new Claim("api_key_id", key.Id.ToString()),
};
```

- [ ] **Step 2: Build check**

```bash
cd packages/api && dotnet build
```

Expected: success.

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Auth/ApiKeyMiddleware.cs
git commit -m "feat: update ApiKeyMiddleware to inject OID claim for EntraOid identity"
```

---

### Task 15: Mass rename Auth0Sub → EntraOid in all source + test files

This is a global find-replace. `Auth0Sub` appears in ~15 feature source files and ~20 test files.

- [ ] **Step 1: Run sed rename on source files**

```bash
find packages/api/src -name "*.cs" | xargs sed -i 's/\.Auth0Sub/\.EntraOid/g'
find packages/api/src -name "*.cs" | xargs sed -i 's/Auth0Sub ==/EntraOid ==/g'
find packages/api/src -name "*.cs" | xargs sed -i 's/Auth0Sub =/EntraOid =/g'
find packages/api/src -name "*.cs" | xargs sed -i 's/Auth0Sub\.StartsWith/EntraOid.StartsWith/g'
```

- [ ] **Step 2: Run sed rename on test files**

```bash
find packages/api/tests -name "*.cs" | xargs sed -i 's/\.Auth0Sub/\.EntraOid/g'
find packages/api/tests -name "*.cs" | xargs sed -i 's/Auth0Sub ==/EntraOid ==/g'
find packages/api/tests -name "*.cs" | xargs sed -i 's/Auth0Sub =/EntraOid =/g'
find packages/api/tests -name "*.cs" | xargs sed -i 's/Auth0Sub\.StartsWith/EntraOid.StartsWith/g'
find packages/api/tests -name "*.cs" | xargs sed -i 's/Auth0Sub\.Returns/EntraOid.Returns/g'
```

- [ ] **Step 3: Verify no remaining Auth0Sub references (except migrations)**

```bash
grep -r "Auth0Sub" packages/api/src --include="*.cs" --exclude-dir=Migrations
grep -r "Auth0Sub" packages/api/tests --include="*.cs"
```

Expected: zero results from both commands.

- [ ] **Step 4: Build and test**

```bash
cd packages/api && dotnet build && dotnet test
```

Expected: build succeeds, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add packages/api/
git commit -m "refactor: global rename Auth0Sub → EntraOid in all feature source and test files"
```

---

### Task 16: Update /api/me endpoint + GetMe handler

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs` (the `/api/me` inline endpoint)
- Modify: `packages/api/src/Tungsten.Api/Features/Auth/GetMe.cs`

The old `/api/me` had complex Auth0-specific email-linking logic. Entra provides a stable OID per user, so we simplify: look up by OID, handle Google-first-login case, preserve the `pending|` email-linking for invited users.

- [ ] **Step 1: Write a failing test for Google pending_activation**

In `packages/api/tests/Tungsten.Api.Tests/Features/Auth/GetMeTests.cs`, add:

```csharp
[Fact]
public async Task Handle_UnknownEntraOid_ReturnsFailure()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    await using var db = new AppDbContext(options);

    var currentUser = Substitute.For<ICurrentUserService>();
    currentUser.EntraOid.Returns("oid-unknown-guid");

    var handler = new GetMe.Handler(db, currentUser);
    var result = await handler.Handle(new GetMe.Query(), CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
}

[Fact]
public async Task Handle_ValidEntraOid_ReturnsUserProfile()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    await using var db = new AppDbContext(options);

    var tenant = new TenantEntity
    {
        Id = Guid.NewGuid(), Name = "Test Corp",
        SchemaPrefix = "tenant_test", Status = "ACTIVE",
    };
    db.Tenants.Add(tenant);

    var oid = Guid.NewGuid().ToString();
    var user = new UserEntity
    {
        Id = Guid.NewGuid(), EntraOid = oid, Email = "test@example.com",
        DisplayName = "Test User", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true,
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    var currentUser = Substitute.For<ICurrentUserService>();
    currentUser.EntraOid.Returns(oid);

    var handler = new GetMe.Handler(db, currentUser);
    var result = await handler.Handle(new GetMe.Query(), CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value.Email.Should().Be("test@example.com");
    result.Value.Role.Should().Be("SUPPLIER");
}
```

- [ ] **Step 2: Run tests to see them fail (GetMe still references Auth0Sub)**

```bash
cd packages/api && dotnet test --filter "GetMeTests"
```

Expected: FAIL (compilation error due to `Auth0Sub`/`EntraOid` mismatch if Task 15 isn't done yet — run after Task 15).

- [ ] **Step 3: Update GetMe.cs**

The property rename from Task 15 handles `currentUser.Auth0Sub` → `currentUser.EntraOid`. Verify `GetMe.cs` now reads:
```csharp
.FirstOrDefaultAsync(u => u.EntraOid == currentUser.EntraOid, ct);
```

No other logic changes needed in `GetMe.Handler`.

- [ ] **Step 4: Replace /api/me endpoint in Program.cs**

Replace the entire inline `/api/me` endpoint (lines 224–301 in original) with:

```csharp
app.MapGet("/api/me", async (
    HttpContext httpContext,
    IMediator mediator,
    AppDbContext db,
    ICurrentUserService currentUser,
    ILogger<Program> logger) =>
{
    try
    {
        var oid = currentUser.EntraOid;

        // Extract email and name from token claims
        var email = httpContext.User.FindFirst("email")?.Value
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = httpContext.User.FindFirst("name")?.Value
            ?? httpContext.User.FindFirst("preferred_username")?.Value
            ?? "User";

        // Check if invited user with this email is waiting to be linked (pending| prefix)
        if (!string.IsNullOrEmpty(email))
        {
            var invited = await db.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.EntraOid.StartsWith("pending|"));
            if (invited is not null)
            {
                invited.EntraOid = oid;
                invited.DisplayName = name;
                invited.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        var meResult = await mediator.Send(new GetMe.Query());
        if (meResult.IsSuccess)
            return Results.Ok(meResult.Value);

        // Not found — check if this is a Google social login (first time)
        var idp = httpContext.User.FindFirst("idp")?.Value
            ?? httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/identityprovider")?.Value;
        var isGoogleLogin = idp?.Contains("google", StringComparison.OrdinalIgnoreCase) == true;

        if (isGoogleLogin && !string.IsNullOrEmpty(email))
        {
            // First-time Google user — provision a pending record, require admin activation
            var existingByEmail = await db.Users.AnyAsync(u => u.Email == email);
            if (!existingByEmail)
            {
                return Results.Json(new { status = "pending_activation" }, statusCode: 403);
            }
        }

        return Results.Json(new { error = "No account found. Contact your administrator to get access." }, statusCode: 403);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "/api/me failed for user");
        return Results.Json(new { error = $"Login failed: {ex.GetType().Name}: {ex.Message}" }, statusCode: 500);
    }
}).RequireAuthorization();
```

- [ ] **Step 5: Run all tests**

```bash
cd packages/api && dotnet test
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Program.cs
git add packages/api/src/Tungsten.Api/Features/Auth/GetMe.cs
git add packages/api/tests/Tungsten.Api.Tests/Features/Auth/GetMeTests.cs
git commit -m "feat: update /api/me and GetMe handler for Entra OID — add Google pending_activation"
```

---

### Task 17: Create EF Core migration for column rename

**Files:**
- Create: `packages/api/src/Tungsten.Api/Migrations/<timestamp>_RenameAuth0SubToEntraOid.cs`

- [ ] **Step 1: Add migration**

```bash
cd packages/api/src/Tungsten.Api
dotnet ef migrations add RenameAuth0SubToEntraOid
```

This auto-generates the migration. EF Core detects the `HasColumnName("entra_oid")` config and generates a `RenameColumn` operation.

- [ ] **Step 2: Verify the generated migration**

Open the generated `.cs` file and confirm it contains:
```csharp
migrationBuilder.RenameColumn(
    name: "Auth0Sub",
    table: "users",
    newName: "entra_oid");

migrationBuilder.RenameIndex(
    name: "IX_users_Auth0Sub",
    table: "users",
    newName: "IX_users_EntraOid");
```

If EF generates `DropColumn` + `AddColumn` instead of `RenameColumn`, manually edit the migration to use `RenameColumn` — this preserves existing data.

- [ ] **Step 3: Verify the Down method**

The `Down` method should reverse the rename:
```csharp
migrationBuilder.RenameColumn(
    name: "entra_oid",
    table: "users",
    newName: "Auth0Sub");
```

- [ ] **Step 4: Build check**

```bash
cd packages/api && dotnet build
```

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Migrations/
git commit -m "feat: add EF Core migration to rename auth0_sub column to entra_oid"
```

---

### Task 18: Final verification

- [ ] **Step 1: Zero auth0 references in Angular source**

```bash
grep -r "auth0" packages/web/src --include="*.ts"
```

Expected: 0 results.

- [ ] **Step 2: Zero Auth0 references in C# source (excluding migrations)**

```bash
grep -r "auth0\|Auth0" packages/api/src --include="*.cs" --exclude-dir=Migrations
```

Expected: 0 results.

- [ ] **Step 3: ng build passes**

```bash
cd packages/web && ng build 2>&1 | tail -5
```

Expected: `Build at: ... - Hash: ...`

- [ ] **Step 4: dotnet build + test passes**

```bash
cd packages/api && dotnet build && dotnet test
```

Expected: Build succeeded, all tests pass.

- [ ] **Step 5: Confirm package.json has no auth0**

```bash
grep "auth0" packages/web/package.json
```

Expected: 0 results.

- [ ] **Step 6: Confirm Microsoft.Identity.Web in csproj**

```bash
grep "Microsoft.Identity.Web" packages/api/src/Tungsten.Api/Tungsten.Api.csproj
```

Expected: 1 result.

- [ ] **Step 7: Commit final state**

```bash
git add .
git commit -m "chore: final verification pass — Auth0 fully replaced with Entra External ID"
```

---

## Post-Migration: Render Environment Variables

Before deploying, set the following in the Render dashboard:

### Frontend (Static Site)
| Variable | Description |
|----------|-------------|
| `MSAL_CLIENT_ID` | Entra app registration client ID (frontend SPA) |
| `MSAL_AUTHORITY` | Full CIAM authority URL, e.g. `https://contoso.ciamlogin.com/contoso.onmicrosoft.com` |
| `MSAL_REDIRECT_URI` | Production redirect, e.g. `https://auditraks.com/login` |
| `API_CLIENT_ID` | Entra app registration client ID (API) |

### Backend (Web Service)
| Variable | Description |
|----------|-------------|
| `AzureAd__Instance` | e.g. `https://contoso.ciamlogin.com/` |
| `AzureAd__TenantId` | Entra tenant ID (GUID) |
| `AzureAd__ClientId` | API app registration client ID |
| `AzureAd__Audience` | e.g. `api://<API_CLIENT_ID>` |

> Render uses `__` as the config section separator. `AzureAd__TenantId` maps to `appsettings["AzureAd:TenantId"]`.
