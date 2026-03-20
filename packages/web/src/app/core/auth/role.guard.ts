import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function roleGuard(...allowedRoles: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    const role = auth.role();
    if (!role) return router.parseUrl('/login');

    if (role === 'PLATFORM_ADMIN' || allowedRoles.includes(role)) return true;

    return router.parseUrl('/unauthorized');
  };
}
