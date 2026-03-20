import { HttpErrorResponse } from '@angular/common/http';

export function extractErrorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    return err.error?.error ?? err.error?.message ?? err.message ?? 'Unknown server error';
  }
  if (err instanceof Error) return err.message;
  return 'An unexpected error occurred';
}
