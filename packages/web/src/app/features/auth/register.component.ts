import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register',
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
              <h1 class="text-xl font-bold text-slate-900 mb-3">Check your email</h1>
              <p class="text-sm text-slate-500 mb-6">We sent a confirmation link to your email. Click it to activate your account.</p>
              <a routerLink="/login" class="text-sm text-indigo-600 hover:underline">Go to sign in</a>
            </div>
          } @else {
            <h1 class="text-xl font-bold text-slate-900 text-center mb-2">Create your account</h1>
            <p class="text-sm text-slate-500 text-center mb-6">Set a password for your invited account.</p>

            @if (errorMessage()) {
              <div class="mb-5 bg-rose-50 border border-rose-200 rounded-xl p-4">
                <p class="text-sm text-rose-700">{{ errorMessage() }}</p>
              </div>
            }

            <form (ngSubmit)="onSubmit()" class="space-y-4">
              <div>
                <label for="email" class="block text-sm font-medium text-slate-700 mb-1">Email</label>
                <input
                  id="email" name="email" type="email"
                  [(ngModel)]="email"
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm bg-slate-50 text-slate-600"
                  [readonly]="!!prefilledEmail"
                />
              </div>
              <div>
                <label for="displayName" class="block text-sm font-medium text-slate-700 mb-1">Display name</label>
                <input
                  id="displayName" name="displayName" type="text"
                  [(ngModel)]="displayName" required
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
              </div>
              <div>
                <label for="password" class="block text-sm font-medium text-slate-700 mb-1">Password</label>
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
                [disabled]="submitting() || password !== confirmPassword || !email || !displayName"
                class="w-full bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 transition-all disabled:opacity-50"
              >
                Create account
              </button>
            </form>

            <div class="mt-4 text-center">
              <a routerLink="/login" class="text-sm text-indigo-600 hover:underline">Already have an account? Sign in</a>
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class RegisterComponent {
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);

  prefilledEmail = this.route.snapshot.queryParamMap.get('email') ?? '';
  email = this.prefilledEmail;
  displayName = '';
  password = '';
  confirmPassword = '';
  readonly submitting = signal(false);
  readonly errorMessage = signal('');
  readonly success = signal(false);

  onSubmit() {
    if (!this.email || !this.password || this.password !== this.confirmPassword || this.submitting()) return;
    this.submitting.set(true);
    this.errorMessage.set('');

    this.auth.register(this.email, this.password, this.displayName).subscribe({
      next: () => this.success.set(true),
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(err?.error?.error || 'Registration failed.');
      },
    });
  }
}
