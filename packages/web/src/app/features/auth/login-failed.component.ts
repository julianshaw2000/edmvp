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
