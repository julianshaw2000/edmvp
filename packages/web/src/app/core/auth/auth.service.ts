import { Injectable, inject, signal, computed } from '@angular/core';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { API_URL } from '../http/api-url.token';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  role: 'SUPPLIER' | 'BUYER' | 'PLATFORM_ADMIN' | 'TENANT_ADMIN';
  tenantId: string;
  tenantName: string;
  tenantStatus: string;
  trialEndsAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private auth0 = inject(Auth0Service);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  readonly isAuthenticated = toSignal(this.auth0.isAuthenticated$, { initialValue: false });
  readonly isLoading = toSignal(this.auth0.isLoading$, { initialValue: true });

  private _profile = signal<UserProfile | null>(null);
  private _profileLoading = signal(false);
  private _profileError = signal<string | null>(null);
  readonly profile = this._profile.asReadonly();
  readonly profileLoading = this._profileLoading.asReadonly();
  readonly profileError = this._profileError.asReadonly();
  readonly role = computed(() => this._profile()?.role ?? null);

  login() {
    this.auth0.loginWithRedirect();
  }

  logout() {
    this.auth0.logout({ logoutParams: { returnTo: window.location.origin } });
  }

  loadProfile(): Promise<UserProfile | null> {
    this._profileLoading.set(true);
    this._profileError.set(null);
    return new Promise(resolve => {
      this.http.get<UserProfile>(`${this.apiUrl}/api/me`).pipe(
        catchError((err) => {
          if (err?.status === 403) {
            this._profileError.set('No account found. Contact your administrator to get access.');
          } else {
            this._profileError.set('Failed to load profile. The server may be starting up — please wait a moment.');
          }
          return of(null);
        })
      ).subscribe(profile => {
        this._profile.set(profile);
        this._profileLoading.set(false);
        resolve(profile);
      });
    });
  }
}
