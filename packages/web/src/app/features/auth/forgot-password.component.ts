import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
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
          @if (sent()) {
            <div class="text-center">
              <h1 class="text-xl font-bold text-slate-900 mb-3">Check your email</h1>
              <p class="text-sm text-slate-500 mb-6">If an account exists for that email, we sent a password reset link.</p>
              <a routerLink="/login" class="text-sm text-indigo-600 hover:underline">Back to sign in</a>
            </div>
          } @else {
            <h1 class="text-xl font-bold text-slate-900 text-center mb-2">Forgot password?</h1>
            <p class="text-sm text-slate-500 text-center mb-6">Enter your email and we'll send a reset link.</p>

            <form (ngSubmit)="onSubmit()" class="space-y-4">
              <div>
                <label for="email" class="block text-sm font-medium text-slate-700 mb-1">Email</label>
                <input
                  id="email" name="email" type="email"
                  [(ngModel)]="email" required
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
              </div>
              <button
                type="submit"
                [disabled]="submitting()"
                class="w-full bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 transition-all disabled:opacity-50"
              >
                Send reset link
              </button>
            </form>

            <div class="mt-4 text-center">
              <a routerLink="/login" class="text-sm text-indigo-600 hover:underline">Back to sign in</a>
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class ForgotPasswordComponent {
  private auth = inject(AuthService);
  email = '';
  readonly submitting = signal(false);
  readonly sent = signal(false);

  onSubmit() {
    if (!this.email || this.submitting()) return;
    this.submitting.set(true);
    this.auth.forgotPassword(this.email).subscribe({
      next: () => this.sent.set(true),
      error: () => this.sent.set(true),
    });
  }
}
