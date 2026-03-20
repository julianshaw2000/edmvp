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
  role: 'SUPPLIER' | 'BUYER' | 'PLATFORM_ADMIN';
  tenantId: string;
  tenantName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private auth0 = inject(Auth0Service);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  readonly isAuthenticated = toSignal(this.auth0.isAuthenticated$, { initialValue: false });
  readonly isLoading = toSignal(this.auth0.isLoading$, { initialValue: true });

  private _profile = signal<UserProfile | null>(null);
  readonly profile = this._profile.asReadonly();
  readonly role = computed(() => this._profile()?.role ?? null);

  login() {
    this.auth0.loginWithRedirect();
  }

  logout() {
    this.auth0.logout({ logoutParams: { returnTo: window.location.origin } });
  }

  loadProfile() {
    this.http.get<UserProfile>(`${this.apiUrl}/api/me`).pipe(
      catchError(() => of(null))
    ).subscribe(profile => {
      this._profile.set(profile);
    });
  }
}
