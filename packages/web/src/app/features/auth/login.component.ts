import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { API_URL } from '../../core/http/api-url.token';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterLink, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50 px-6">
      <div class="w-full max-w-sm">
        <div class="flex items-center gap-2 justify-center mb-8">
          <img src="assets/auditraks-logo.png" alt="auditraks" class="h-10" />
        </div>

        <div class="bg-white rounded-2xl shadow-sm border border-slate-200 p-8">
          <h1 class="text-xl font-bold text-slate-900 text-center mb-6">Sign in</h1>

          @if (alreadySetupBanner()) {
            <div class="mb-5 bg-blue-50 border border-blue-200 rounded-xl p-4">
              <p class="text-sm text-blue-700">Your account is already set up. Please sign in.</p>
            </div>
          }

          @if (emailConfirmed()) {
            <div class="mb-5 bg-emerald-50 border border-emerald-200 rounded-xl p-4">
              <p class="text-sm text-emerald-700">Email confirmed! You can now sign in.</p>
            </div>
          }

          @if (errorMessage()) {
            <div class="mb-5 bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
              <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p class="text-sm text-rose-700">{{ errorMessage() }}</p>
            </div>
          }

          @if (incompleteSetup()) {
            <div class="mb-5 bg-amber-50 border border-amber-200 rounded-xl p-4">
              @if (resendSuccess()) {
                <p class="text-sm text-amber-700">Setup email sent. Check your inbox.</p>
              } @else {
                <p class="text-sm text-amber-700">
                  Your account setup is incomplete. Check your email for a setup link, or
                  <button (click)="resendSetupEmail()" class="underline font-medium">Resend setup email</button>.
                </p>
              }
            </div>
          }

          <form (ngSubmit)="onSubmit()" class="space-y-4">
            <div>
              <label for="email" class="block text-sm font-medium text-slate-700 mb-1">Email</label>
              <input
                id="email" name="email" type="email"
                [(ngModel)]="email" required
                class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                placeholder="you@company.com"
              />
            </div>

            <div>
              <label for="password" class="block text-sm font-medium text-slate-700 mb-1">Password</label>
              <input
                id="password" name="password" type="password"
                [(ngModel)]="password" required
                class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              />
            </div>

            <button
              type="submit"
              [disabled]="submitting()"
              class="w-full bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 transition-all disabled:opacity-50"
            >
              @if (submitting()) {
                <div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin inline-block mr-2"></div>
                Signing in...
              } @else {
                Sign in
              }
            </button>
          </form>

          <div class="mt-4 text-center">
            <a routerLink="/forgot-password" class="text-sm text-indigo-600 hover:underline">
              Forgot password?
            </a>
          </div>

          <div class="mt-6 pt-5 border-t border-slate-100 text-center">
            <a routerLink="/signup" class="text-sm text-indigo-600 hover:underline">
              Don't have an account? Start a free trial
            </a>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  email = '';
  password = '';
  readonly submitting = signal(false);
  readonly errorMessage = signal('');
  readonly emailConfirmed = signal(false);
  readonly alreadySetupBanner = signal(false);
  readonly incompleteSetup = signal(false);
  readonly resendSuccess = signal(false);

  constructor() {
    const params = this.route.snapshot.queryParamMap;
    if (params.get('emailConfirmed') === 'true') {
      this.emailConfirmed.set(true);
    }
    if (params.get('hint') === 'already-setup') {
      this.alreadySetupBanner.set(true);
    }
  }

  onSubmit() {
    if (!this.email || !this.password || this.submitting()) return;

    this.submitting.set(true);
    this.errorMessage.set('');

    this.auth.login(this.email, this.password).subscribe({
      next: async (response) => {
        this.auth.setAccessToken(response.accessToken);
        const profile = await this.auth.loadProfile();
        if (profile) {
          this.navigateByRole(profile.role);
        } else {
          this.submitting.set(false);
          this.errorMessage.set(this.auth.profileError() || 'Failed to load profile.');
        }
      },
      error: (err) => {
        this.submitting.set(false);
        const errorCode = err?.error?.error;
        if (errorCode === 'ACCOUNT_SETUP_INCOMPLETE') {
          this.incompleteSetup.set(true);
        } else {
          this.errorMessage.set(errorCode || 'Sign in failed. Please try again.');
        }
      },
    });
  }

  resendSetupEmail() {
    this.http.post(`${this.apiUrl}/api/signup/resend-setup`, { email: this.email }).subscribe({
      next: () => this.resendSuccess.set(true),
      error: () => this.resendSuccess.set(true), // always show success (privacy-safe)
    });
  }

  private navigateByRole(role: string) {
    if (role === 'SUPPLIER') this.router.navigate(['/supplier']);
    else if (role === 'BUYER') this.router.navigate(['/buyer']);
    else if (role === 'PLATFORM_ADMIN' || role === 'TENANT_ADMIN') this.router.navigate(['/admin']);
    else this.router.navigate(['/supplier']);
  }
}
