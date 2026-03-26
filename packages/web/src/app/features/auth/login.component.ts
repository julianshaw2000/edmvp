import { Component, inject, OnInit, DestroyRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, take, distinctUntilChanged } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
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
export class LoginComponent implements OnInit {
  protected auth = inject(AuthService);
  private msal = inject(MsalService);
  private broadcastService = inject(MsalBroadcastService);
  private router = inject(Router);
  private apiUrl = inject(API_URL);
  private destroyRef = inject(DestroyRef);
  loadingMessage = 'Checking authentication...';
  errorMessage = '';

  ngOnInit() {
    // Process any pending redirect (e.g. post-logout return) BEFORE subscribing to
    // inProgress$. Without this, inProgress$ emits its initial 'None' value before
    // handleRedirectPromise clears the stale interaction flag, causing
    // interaction_in_progress errors when the user tries to log in again.
    this.msal.handleRedirectObservable().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => this.watchAuthState(),
      error: () => this.watchAuthState(),
    });
  }

  private watchAuthState() {
    this.broadcastService.inProgress$
      .pipe(
        distinctUntilChanged(),
        filter(status => status === InteractionStatus.None),
        take(1),
        takeUntilDestroyed(this.destroyRef),
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
