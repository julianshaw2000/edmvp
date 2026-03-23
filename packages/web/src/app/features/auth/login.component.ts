import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { AuthService } from '../../core/auth/auth.service';
import { API_URL } from '../../core/http/api-url.token';
import { filter, take, switchMap } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen flex">
      <!-- Left: Branding -->
      <div class="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-indigo-700 via-indigo-600 to-indigo-800 relative overflow-hidden">
        <div class="absolute inset-0 opacity-10">
          <svg class="w-full h-full" viewBox="0 0 800 800" fill="none">
            <circle cx="400" cy="400" r="300" stroke="white" stroke-width="0.5" />
            <circle cx="400" cy="400" r="200" stroke="white" stroke-width="0.5" />
            <circle cx="400" cy="400" r="100" stroke="white" stroke-width="0.5" />
            <line x1="100" y1="400" x2="700" y2="400" stroke="white" stroke-width="0.5" />
            <line x1="400" y1="100" x2="400" y2="700" stroke="white" stroke-width="0.5" />
          </svg>
        </div>
        <div class="relative z-10 flex flex-col justify-center px-16">
          <div class="flex items-center gap-3 mb-8">
            <div class="w-10 h-10 bg-white/20 backdrop-blur rounded-xl flex items-center justify-center">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <span class="text-2xl font-bold text-white">auditraks</span>
          </div>
          <h2 class="text-4xl font-bold text-white leading-tight mb-4">
            Supply Chain<br />Compliance,<br />Simplified.
          </h2>
          <p class="text-indigo-200 text-lg max-w-md leading-relaxed">
            Track mineral custody from mine to refinery with RMAP and OECD DDG compliance built in. Real-time visibility for every stakeholder.
          </p>
          <div class="mt-12 flex items-center gap-8">
            <div>
              <p class="text-3xl font-bold text-white">100%</p>
              <p class="text-sm text-indigo-300 mt-1">Audit Ready</p>
            </div>
            <div class="w-px h-12 bg-indigo-500/50"></div>
            <div>
              <p class="text-3xl font-bold text-white">SHA-256</p>
              <p class="text-sm text-indigo-300 mt-1">Tamper Evidence</p>
            </div>
            <div class="w-px h-12 bg-indigo-500/50"></div>
            <div>
              <p class="text-3xl font-bold text-white">RMAP</p>
              <p class="text-sm text-indigo-300 mt-1">Verified</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Right: Login Card -->
      <div class="flex-1 flex items-center justify-center bg-slate-50 px-6">
        <div class="w-full max-w-sm">
          <!-- Mobile logo -->
          <div class="lg:hidden flex items-center gap-3 mb-10 justify-center">
            <div class="w-10 h-10 bg-indigo-600 rounded-xl flex items-center justify-center">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <span class="text-2xl font-bold text-slate-900">auditraks</span>
          </div>

          <div class="bg-white rounded-2xl shadow-sm border border-slate-200 p-8">
            <div class="text-center mb-8">
              <h1 class="text-xl font-bold text-slate-900">Welcome back</h1>
              <p class="text-sm text-slate-500 mt-1">Sign in to your compliance dashboard</p>
            </div>

            @if (checking) {
              <div class="flex flex-col items-center py-6">
                <div class="w-8 h-8 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin mb-3"></div>
                <p class="text-sm text-slate-500">{{ loadingMessage }}</p>
              </div>
            } @else {
              @if (auth.profileError()) {
                <div class="mb-5 bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
                  <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <p class="text-sm text-rose-700">{{ auth.profileError() }}</p>
                </div>
              }
              <button
                (click)="auth.login()"
                class="w-full flex items-center justify-center gap-3 bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 shadow-sm shadow-indigo-600/20 transition-all duration-150"
              >
                <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 01-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z" fill="#fff" opacity="0.8"/>
                  <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#fff" opacity="0.9"/>
                  <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#fff" opacity="0.7"/>
                  <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#fff" opacity="0.8"/>
                </svg>
                Sign in with Google
              </button>

              <div class="mt-6 text-center space-y-2">
                <p class="text-xs text-slate-400">
                  Secured by Auth0. By signing in, you agree to our terms of service.
                </p>
                <a routerLink="/signup" class="text-sm text-indigo-600 hover:underline">
                  Don't have an account? Start a free trial
                </a>
              </div>
            }
          </div>
        </div>
      </div>
    </div>
  `,
})
export class LoginComponent implements OnInit, OnDestroy {
  protected auth = inject(AuthService);
  private auth0 = inject(Auth0Service);
  private router = inject(Router);
  private apiUrl = inject(API_URL);
  checking = true;
  loadingMessage = 'Checking authentication...';
  private destroyed = false;

  ngOnInit() {
    this.auth0.isLoading$.pipe(
      filter(loading => !loading),
      take(1),
      switchMap(() => this.auth0.isAuthenticated$),
      take(1),
    ).subscribe(async isAuthenticated => {
      if (isAuthenticated) {
        this.loadingMessage = 'Loading your profile...';
        const profile = await this.auth.loadProfile();
        if (profile) {
          this.navigateByRole(profile.role);
        } else if (this.auth.profileError()?.startsWith('No account found')) {
          // 403 — user exists in Auth0 but has no platform account; do not retry
          this.checking = false;
          this.loadingMessage = '';
        } else {
          // Backend may be cold-starting — poll health until ready, then retry
          this.loadingMessage = 'Server is starting up, please wait...';
          await this.waitForBackend();
          if (this.destroyed) return;
          this.loadingMessage = 'Loading your profile...';
          const retry = await this.auth.loadProfile();
          if (retry) {
            this.navigateByRole(retry.role);
          } else {
            this.checking = false;
            this.loadingMessage = '';
          }
        }
      } else {
        this.checking = false;
      }
    });
  }

  ngOnDestroy() {
    this.destroyed = true;
  }

  private navigateByRole(role: string) {
    this.loadingMessage = 'Redirecting...';
    if (role === 'SUPPLIER') this.router.navigate(['/supplier']);
    else if (role === 'BUYER') this.router.navigate(['/buyer']);
    else if (role === 'PLATFORM_ADMIN' || role === 'TENANT_ADMIN') this.router.navigate(['/admin']);
    else this.router.navigate(['/supplier']);
  }

  /** Poll /health every 2s until backend reports "healthy" (max 30s). */
  private async waitForBackend(): Promise<void> {
    const maxAttempts = 15;
    for (let i = 0; i < maxAttempts && !this.destroyed; i++) {
      try {
        const res = await fetch(`${this.apiUrl}/health`);
        if (res.ok) {
          const body = await res.json();
          if (body.status === 'healthy') return;
        }
      } catch { /* server not up yet */ }
      this.loadingMessage = `Server is starting up, please wait... (${i + 1}/${maxAttempts})`;
      await new Promise(r => setTimeout(r, 2000));
    }
  }
}
