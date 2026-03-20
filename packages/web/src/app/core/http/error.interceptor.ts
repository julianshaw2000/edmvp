import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      console.error(`[HTTP Error] ${err.status} ${req.method} ${req.url}`, err.error);
      return throwError(() => err);
    })
  );
