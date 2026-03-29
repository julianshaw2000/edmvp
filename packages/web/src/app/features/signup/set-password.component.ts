import { Component, inject, signal, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { timer, EMPTY } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { API_URL } from '../../core/http/api-url.token';

const POLL_INTERVAL_MS = 2000;
const POLL_TIMEOUT_MS = 30_000;

type SetPasswordState = 'provisioning' | 'ready' | 'submitting' | 'timeout' | 'error';

@Component({
  selector: 'app-set-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50 px-6">
      <div class="w-full max-w-sm">
        <div class="flex items-center gap-2 justify-center mb-8">
          <img src="assets/auditraks-logo.png" alt="auditraks" class="h-10" />
        </div>

        <div class="bg-white rounded-2xl shadow-sm border border-slate-200 p-8">

          @if (state() === 'provisioning') {
            <div class="flex flex-col items-center justify-center py-6 gap-4">
              <div class="w-8 h-8 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
              <p class="text-sm text-slate-600 text-center">Setting up your account…</p>
            </div>
          }

          @if (state() === 'timeout') {
            <div class="text-center py-4">
              <p class="text-sm text-rose-700">
                Something went wrong setting up your account. Please
                <a href="mailto:support@accutrac.org" class="underline font-medium">contact support</a>.
              </p>
            </div>
          }

          @if (state() === 'error') {
            <div class="mb-5 bg-rose-50 border border-rose-200 rounded-xl p-4">
              <p class="text-sm text-rose-700">{{ errorMessage() }}</p>
            </div>
          }

          @if (state() === 'ready' || state() === 'submitting') {
            <div class="text-center mb-6">
              <h1 class="text-xl font-bold text-slate-900">Set your password</h1>
              <p class="text-sm text-slate-500 mt-1">Create a password to complete your account setup.</p>
            </div>

            @if (passwordError()) {
              <div class="mb-4 bg-rose-50 border border-rose-200 rounded-xl p-3">
                <p class="text-sm text-rose-700">{{ passwordError() }}</p>
              </div>
            }

            <form (ngSubmit)="onSubmit()" class="space-y-4">
              <div>
                <label for="password" class="block text-sm font-medium text-slate-700 mb-1">Password</label>
                <input
                  id="password"
                  name="password"
                  type="password"
                  [(ngModel)]="password"
                  required
                  [disabled]="state() === 'submitting'"
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent disabled:opacity-50"
                  placeholder="Create a strong password"
                />
                <p class="text-xs text-slate-400 mt-1.5">Min 8 characters · Uppercase · Lowercase · Number</p>
              </div>

              <div>
                <label for="confirmPassword" class="block text-sm font-medium text-slate-700 mb-1">Confirm Password</label>
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  [(ngModel)]="confirmPassword"
                  required
                  [disabled]="state() === 'submitting'"
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent disabled:opacity-50"
                  placeholder="Repeat your password"
                />
              </div>

              <button
                type="submit"
                [disabled]="state() === 'submitting'"
                class="w-full flex items-center justify-center gap-2 bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 shadow-sm shadow-indigo-600/20 transition-all duration-150 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                @if (state() === 'submitting') {
                  <div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  Setting up…
                } @else {
                  Set Password & Continue
                }
              </button>
            </form>
          }

        </div>
      </div>
    </div>
  `,
})
export class SetPasswordComponent {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  readonly state = signal<SetPasswordState>('provisioning');
  readonly errorMessage = signal<string>('');
  readonly passwordError = signal<string>('');

  password = '';
  confirmPassword = '';

  private sessionId = '';
  private startTime = 0;

  constructor() {
    const params = this.route.snapshot.queryParamMap;
    const session = params.get('session');

    if (!session) {
      this.router.navigate(['/signup']);
      return;
    }

    this.sessionId = session;
    this.startTime = Date.now();

    timer(0, POLL_INTERVAL_MS)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() =>
          this.http
            .get<{ provisioned: boolean }>(`${this.apiUrl}/api/signup/session/${this.sessionId}`)
            .pipe(catchError(() => {
              if (Date.now() - this.startTime > POLL_TIMEOUT_MS) {
                this.state.set('timeout');
              }
              return EMPTY;
            })),
        ),
      )
      .subscribe(res => {
        if (res.provisioned === true) {
          this.state.set('ready');
        } else if (Date.now() - this.startTime > POLL_TIMEOUT_MS) {
          this.state.set('timeout');
        }
      });
  }

  onSubmit() {
    this.passwordError.set('');

    const pwd = this.password;
    if (pwd.length < 8 || !/[A-Z]/.test(pwd) || !/[a-z]/.test(pwd) || !/[0-9]/.test(pwd)) {
      this.passwordError.set('Password must be at least 8 characters and include uppercase, lowercase, and a number.');
      return;
    }

    if (pwd !== this.confirmPassword) {
      this.passwordError.set('Passwords do not match.');
      return;
    }

    this.state.set('submitting');

    this.http
      .post<{ accessToken: string }>(`${this.apiUrl}/api/signup/set-password`, {
        sessionId: this.sessionId,
        password: pwd,
      }, { withCredentials: true })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: async res => {
          this.authService.setAccessToken(res.accessToken);
          await this.authService.loadProfile();
          this.router.navigate(['/admin']);
        },
        error: err => {
          if (err?.status === 409) {
            this.router.navigate(['/login'], { queryParams: { hint: 'already-setup' } });
          } else {
            this.state.set('error');
            this.errorMessage.set(err?.error?.error || 'An error occurred. Please contact support.');
          }
        },
      });
  }
}
