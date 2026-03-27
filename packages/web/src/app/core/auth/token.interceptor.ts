import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  // Only attach token to our own API, skip auth endpoints
  if (!req.url.startsWith(environment.apiUrl) || req.url.includes('/api/auth/')) {
    return next(req);
  }

  const auth = inject(AuthService);
  const token = auth.accessToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  }

  return next(req);
};
