import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) {
    if (!auth.profile()) {
      const profile = await auth.loadProfile();
      if (!profile) return router.parseUrl('/login');
    }
    return true;
  }

  // Try refreshing token (handles page refresh — refresh cookie sent automatically)
  const refreshed = await auth.tryRefresh();
  if (refreshed) {
    const profile = await auth.loadProfile();
    if (profile) return true;
  }

  return router.parseUrl('/login');
};
