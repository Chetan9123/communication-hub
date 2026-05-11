import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        toast.error('Session Expired', 'Please log in again.');
        auth.logout();
      } else if (error.status === 403) {
        toast.error('Access Denied', 'You do not have permission for this action.');
      } else if (error.status === 500) {
        toast.error('Server Error', 'An unexpected error occurred on the server.');
      } else if (error.status === 0) {
        toast.error('Network Error', 'Cannot connect to the backend server.');
      }
      return throwError(() => error);
    })
  );
};
