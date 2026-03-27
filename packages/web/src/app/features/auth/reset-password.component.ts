import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-reset-password',
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
          @if (success()) {
            <div class="text-center">
              <h1 class="text-xl font-bold text-slate-900 mb-3">Password reset!</h1>
              <p class="text-sm text-slate-500 mb-6">Your password has been updated. You can now sign in.</p>
              <a routerLink="/login" class="inline-block bg-indigo-600 text-white py-3 px-6 rounded-xl font-medium hover:bg-indigo-700 transition-all">
                Sign in
              </a>
            </div>
          } @else {
            <h1 class="text-xl font-bold text-slate-900 text-center mb-6">Set new password</h1>

            @if (errorMessage()) {
              <div class="mb-5 bg-rose-50 border border-rose-200 rounded-xl p-4">
                <p class="text-sm text-rose-700">{{ errorMessage() }}</p>
              </div>
            }

            <form (ngSubmit)="onSubmit()" class="space-y-4">
              <div>
                <label for="password" class="block text-sm font-medium text-slate-700 mb-1">New password</label>
                <input
                  id="password" name="password" type="password"
                  [(ngModel)]="password" required minlength="8"
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
                <p class="text-xs text-slate-400 mt-1">Minimum 8 characters with uppercase, lowercase, and a digit.</p>
              </div>
              <div>
                <label for="confirmPassword" class="block text-sm font-medium text-slate-700 mb-1">Confirm password</label>
                <input
                  id="confirmPassword" name="confirmPassword" type="password"
                  [(ngModel)]="confirmPassword" required
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
                @if (confirmPassword && password !== confirmPassword) {
                  <p class="text-xs text-rose-600 mt-1">Passwords do not match.</p>
                }
              </div>
              <button
                type="submit"
                [disabled]="submitting() || password !== confirmPassword"
                class="w-full bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 transition-all disabled:opacity-50"
              >
                Reset password
              </button>
            </form>
          }
        </div>
      </div>
    </div>
  `,
})
export class ResetPasswordComponent {
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);

  password = '';
  confirmPassword = '';
  readonly submitting = signal(false);
  readonly errorMessage = signal('');
  readonly success = signal(false);

  private email = this.route.snapshot.queryParamMap.get('email') ?? '';
  private token = this.route.snapshot.queryParamMap.get('token') ?? '';

  onSubmit() {
    if (!this.password || this.password !== this.confirmPassword || this.submitting()) return;
    this.submitting.set(true);
    this.errorMessage.set('');

    this.auth.resetPassword(this.email, this.token, this.password).subscribe({
      next: () => this.success.set(true),
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(err?.error?.error || 'Reset failed. The link may have expired.');
      },
    });
  }
}
