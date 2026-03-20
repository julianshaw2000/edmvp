import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { map, take, switchMap, of, filter } from 'rxjs';

export const authGuard: CanActivateFn = () => {
  const auth0 = inject(Auth0Service);
  const router = inject(Router);

  // Wait for Auth0 SDK to finish loading, then check authentication
  return auth0.isLoading$.pipe(
    filter(loading => !loading),
    take(1),
    switchMap(() => auth0.isAuthenticated$),
    take(1),
    map(isAuthenticated => {
      if (isAuthenticated) return true;
      auth0.loginWithRedirect();
      return false;
    })
  );
};
