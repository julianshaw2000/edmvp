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
  private auth0 = inject(Auth0Service);
  private router = inject(Router);
  private apiUrl = inject(API_URL);
  loadingMessage = 'Checking authentication...';
  errorMessage = '';
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
          this.errorMessage = 'No account found. Contact your administrator to get access.';
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
            this.errorMessage = this.auth.profileError() || 'Unable to load your profile. Please try again.';
          }
        }
      } else {
        // Not authenticated — redirect to Auth0 hosted login
        this.loadingMessage = 'Redirecting to sign in...';
        this.auth.login();
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
