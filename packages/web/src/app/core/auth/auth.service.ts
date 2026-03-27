import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, of, firstValueFrom } from 'rxjs';
import { API_URL } from '../http/api-url.token';
import { environment } from '../../../environments/environment';

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

interface TokenResponse {
  accessToken: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);
  private router = inject(Router);

  private _accessToken = signal<string | null>(null);
  private _profile = signal<UserProfile | null>(null);
  private _profileLoading = signal(false);
  private _profileError = signal<string | null>(null);
  private _isLoggedIn = signal(false);
  private _refreshInFlight: Promise<boolean> | null = null;

  readonly accessToken = this._accessToken.asReadonly();
  readonly profile = this._profile.asReadonly();
  readonly profileLoading = this._profileLoading.asReadonly();
  readonly profileError = this._profileError.asReadonly();
  readonly isLoggedIn = this._isLoggedIn.asReadonly();
  readonly role = computed(() => this._profile()?.role ?? null);

  login(email: string, password: string) {
    return this.http.post<TokenResponse>(
      `${this.apiUrl}/api/auth/login`,
      { email, password },
      { withCredentials: true },
    );
  }

  setAccessToken(token: string) {
    this._accessToken.set(token);
    this._isLoggedIn.set(true);
  }

  logout() {
    this.http
      .post(`${this.apiUrl}/api/auth/logout`, {}, { withCredentials: true })
      .subscribe({ error: () => {} });
    this.clearAuth();
    this.router.navigate(['/login']);
  }

  async tryRefresh(): Promise<boolean> {
    if (this._refreshInFlight) return this._refreshInFlight;
    this._refreshInFlight = this._doRefresh();
    const result = await this._refreshInFlight;
    this._refreshInFlight = null;
    return result;
  }

  private async _doRefresh(): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.http.post<TokenResponse>(
          `${this.apiUrl}/api/auth/refresh`,
          {},
          { withCredentials: true },
        ),
      );
      this._accessToken.set(response.accessToken);
      this._isLoggedIn.set(true);
      return true;
    } catch {
      this.clearAuth();
      return false;
    }
  }

  forgotPassword(email: string) {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/api/auth/forgot-password`,
      { email },
    );
  }

  resetPassword(email: string, token: string, newPassword: string) {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/api/auth/reset-password`,
      { email, token, newPassword },
    );
  }

  register(email: string, password: string, displayName: string) {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/api/auth/register`,
      { email, password, displayName },
    );
  }

  resendConfirmation(email: string) {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/api/auth/resend-confirmation`,
      { email },
    );
  }

  loadProfile(): Promise<UserProfile | null> {
    this._profileLoading.set(true);
    this._profileError.set(null);
    return new Promise((resolve) => {
      this.http
        .get<UserProfile>(`${this.apiUrl}/api/me`)
        .pipe(
          catchError((err) => {
            if (err?.status === 401) {
              this._isLoggedIn.set(false);
            } else if (err?.status === 403) {
              this._profileError.set('No account found. Contact your administrator.');
            } else {
              this._profileError.set('Failed to load profile.');
            }
            return of(null);
          }),
        )
        .subscribe((profile) => {
          this._profile.set(profile);
          this._isLoggedIn.set(profile !== null);
          this._profileLoading.set(false);
          resolve(profile);
        });
    });
  }

  private clearAuth() {
    this._accessToken.set(null);
    this._profile.set(null);
    this._isLoggedIn.set(false);
  }
}
