import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { API_URL } from '../../core/http/api-url.token';

@Component({
  selector: 'app-signup',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FormsModule],
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
              <p class="text-3xl font-bold text-white">60 days</p>
              <p class="text-sm text-indigo-300 mt-1">Free Trial</p>
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

      <!-- Right: Signup Card -->
      <div class="flex-1 flex items-center justify-center bg-slate-50 px-6 py-12">
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
            <div class="text-center mb-6">
              <h1 class="text-xl font-bold text-slate-900">Start your free trial</h1>
              <p class="text-sm text-indigo-600 font-medium mt-1">Signing up for: {{ planLabel() }}</p>
              <p class="text-sm text-slate-500 mt-0.5">{{ planPrice() }} after trial. Cancel anytime.</p>
            </div>

            @if (errorMessage()) {
              <div class="mb-5 bg-rose-50 border border-rose-200 rounded-xl p-4 flex items-start gap-3">
                <svg class="w-5 h-5 text-rose-500 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p class="text-sm text-rose-700">{{ errorMessage() }}</p>
              </div>
            }

            <form (ngSubmit)="onSubmit()" #signupForm="ngForm" novalidate class="space-y-4">
              <div>
                <label for="companyName" class="block text-sm font-medium text-slate-700 mb-1">Company name</label>
                <input
                  id="companyName"
                  name="companyName"
                  type="text"
                  [(ngModel)]="companyName"
                  required
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="Acme Mining Co."
                />
              </div>

              <div>
                <label for="yourName" class="block text-sm font-medium text-slate-700 mb-1">Your name</label>
                <input
                  id="yourName"
                  name="yourName"
                  type="text"
                  [(ngModel)]="yourName"
                  required
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="Jane Smith"
                />
              </div>

              <div>
                <label for="email" class="block text-sm font-medium text-slate-700 mb-1">Email address</label>
                <input
                  id="email"
                  name="email"
                  type="email"
                  [(ngModel)]="email"
                  required
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="jane@acmemining.com"
                />
              </div>

              <div>
                <label for="confirmEmail" class="block text-sm font-medium text-slate-700 mb-1">Confirm email</label>
                <input
                  id="confirmEmail"
                  name="confirmEmail"
                  type="email"
                  [(ngModel)]="confirmEmail"
                  required
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="jane@acmemining.com"
                />
                @if (confirmEmail && email !== confirmEmail) {
                  <p class="text-xs text-rose-600 mt-1">Emails do not match.</p>
                }
              </div>

              <button
                type="submit"
                [disabled]="!isFormValid() || submitting()"
                class="w-full flex items-center justify-center gap-2 bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 shadow-sm shadow-indigo-600/20 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                @if (submitting()) {
                  <div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  Processing...
                } @else {
                  Start 60-day free trial
                }
              </button>
            </form>

            <div class="mt-6 text-center">
              <a routerLink="/login" class="text-sm text-indigo-600 hover:underline">
                Already have an account? Sign in
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class SignupComponent {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);
  private route = inject(ActivatedRoute);

  private readonly planParam = toSignal(
    this.route.queryParamMap.pipe(map(p => (p.get('plan') ?? 'pro').toUpperCase())),
    { initialValue: 'PRO' }
  );

  readonly plan = computed(() => this.planParam() === 'STARTER' ? 'STARTER' : 'PRO');
  readonly planLabel = computed(() => this.plan() === 'STARTER' ? 'Starter' : 'Pro');
  readonly planPrice = computed(() => this.plan() === 'STARTER' ? '$99/month' : '$249/month');

  companyName = '';
  yourName = '';
  email = '';
  confirmEmail = '';

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  isFormValid(): boolean {
    return (
      this.companyName.trim().length > 0 &&
      this.yourName.trim().length > 0 &&
      this.email.trim().length > 0 &&
      this.confirmEmail.trim().length > 0 &&
      this.email === this.confirmEmail
    );
  }

  onSubmit() {
    if (!this.isFormValid() || this.submitting()) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.http.post<{ checkoutUrl: string }>(`${this.apiUrl}/api/signup/checkout`, {
      companyName: this.companyName.trim(),
      name: this.yourName.trim(),
      email: this.email.trim(),
      plan: this.plan(),
    }).subscribe({
      next: (res) => {
        window.location.href = res.checkoutUrl;
      },
      error: (err) => {
        this.submitting.set(false);
        const message = err?.error?.error || err?.error?.detail || err?.message || 'Something went wrong. Please try again.';
        if (err?.status === 409 || message.includes('already in use')) {
          this.errorMessage.set('An account with this email already exists. Please sign in instead.');
        } else {
          this.errorMessage.set(message);
        }
      },
    });
  }
}
