import { Injectable, inject, signal, computed } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { HttpClient } from '@angular/common/http';
import { catchError, of } from 'rxjs';
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

@Injectable({ providedIn: 'root' })
export class AuthService {
  private msal = inject(MsalService);
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  private _profile = signal<UserProfile | null>(null);
  private _profileLoading = signal(false);
  private _profileError = signal<string | null>(null);
  readonly profile = this._profile.asReadonly();
  readonly profileLoading = this._profileLoading.asReadonly();
  readonly profileError = this._profileError.asReadonly();
  readonly role = computed(() => this._profile()?.role ?? null);

  isLoggedIn(): boolean {
    return this.msal.instance.getAllAccounts().length > 0;
  }

  login() {
    this.msal.loginRedirect({
      scopes: [`api://${environment.msal.apiClientId}/.default`],
    }).subscribe({
      error: (err: any) => {
        // interaction_in_progress means another auth flow already started — let it proceed
        if (err?.errorCode !== 'interaction_in_progress') console.error('loginRedirect failed', err);
      }
    });
  }

  logout() {
    this.msal.logoutRedirect({
      postLogoutRedirectUri: window.location.origin,
    });
  }

  resetPassword() {
    this.msal.loginRedirect({
      authority: environment.msal.resetPasswordAuthority,
      scopes: [],
    });
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
