import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError, from } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Don't retry auth endpoints or non-401 errors
      if (error.status === 401 && !req.url.includes('/api/auth/')) {
        return from(auth.tryRefresh()).pipe(
          switchMap((refreshed) => {
            if (refreshed) {
              const retryReq = req.clone({
                setHeaders: { Authorization: `Bearer ${auth.accessToken()}` },
              });
              return next(retryReq);
            }
            router.navigate(['/login']);
            return throwError(() => error);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
