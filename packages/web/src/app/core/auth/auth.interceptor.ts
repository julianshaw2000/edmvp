import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService as Auth0Service } from '@auth0/auth0-angular';
import { switchMap, take } from 'rxjs';
import { API_URL } from '../http/api-url.token';

// Public endpoints that should NOT receive auth tokens
const PUBLIC_PATHS = ['/api/signup/', '/api/stripe/webhook'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = inject(API_URL);

  if (!req.url.startsWith(apiUrl)) {
    return next(req);
  }

  // Skip auth for public endpoints
  if (PUBLIC_PATHS.some(path => req.url.includes(path))) {
    return next(req);
  }

  const auth0 = inject(Auth0Service);

  return auth0.getAccessTokenSilently().pipe(
    take(1),
    switchMap(token => {
      const cloned = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      });
      return next(cloned);
    })
  );
};
