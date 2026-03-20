import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { AuthService } from '../../core/auth/auth.service';
import { filter, take, switchMap } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50">
      <div class="bg-white p-8 rounded-xl shadow-sm border border-slate-200 max-w-md w-full text-center">
        <h1 class="text-2xl font-bold text-slate-900 mb-2">Tungsten</h1>
        <p class="text-slate-500 mb-6">Supply Chain Compliance Platform</p>
        @if (checking) {
          <p class="text-slate-400 text-sm">Checking authentication...</p>
        } @else {
          <button
            (click)="auth.login()"
            class="w-full bg-blue-600 text-white py-2.5 px-4 rounded-lg font-medium hover:bg-blue-700 transition-colors"
          >
            Sign in
          </button>
        }
      </div>
    </div>
  `,
})
export class LoginComponent implements OnInit {
  protected auth = inject(AuthService);
  private auth0 = inject(Auth0Service);
  private router = inject(Router);
  checking = true;

  ngOnInit() {
    // Wait for Auth0 to finish loading, then check if already authenticated
    this.auth0.isLoading$.pipe(
      filter(loading => !loading),
      take(1),
      switchMap(() => this.auth0.isAuthenticated$),
      take(1),
    ).subscribe(isAuthenticated => {
      if (isAuthenticated) {
        // Load profile and redirect based on role
        this.auth.loadProfile();
        // Give profile a moment to load, then redirect
        setTimeout(() => {
          const role = this.auth.role();
          if (role === 'SUPPLIER') this.router.navigate(['/supplier']);
          else if (role === 'BUYER') this.router.navigate(['/buyer']);
          else if (role === 'PLATFORM_ADMIN') this.router.navigate(['/admin']);
          else this.router.navigate(['/supplier']); // default
        }, 1500);
      } else {
        this.checking = false;
      }
    });
  }
}
