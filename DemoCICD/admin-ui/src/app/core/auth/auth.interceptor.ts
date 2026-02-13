import { HttpInterceptorFn, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, tap, throwError } from 'rxjs';

import { AuthService } from './auth.service';

/** Header đánh dấu request đã được retry sau refresh (tránh retry vô hạn khi vẫn 401) */
const SKIP_AUTH_RETRY_HEADER = 'X-Skip-Auth-Retry';

/** Path patterns auth (khớp với mọi apiBaseUrl), không gắn Bearer và không retry khi 401 */
const AUTH_REQUEST_PATHS = ['/auth/login', '/auth/refresh-token'];

function isAuthRequest(url: string): boolean {
  return AUTH_REQUEST_PATHS.some((path) => url.includes(path));
}

function addBearerToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) return req;
  return req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  });
}

/**
 * Interceptor: gắn Bearer token, xử lý 401 bằng refresh-token và retry, tránh nhiều refresh song song.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // Request đến login/refresh-token: không gắn token, không xử lý 401 (để login/refresh tự xử)
  if (isAuthRequest(req.url)) {
    return next(req).pipe(
      catchError((err) => {
        if (err instanceof HttpErrorResponse && err.status === 401) {
          // Có thể clear state nếu backend trả 401 cho login (sai mật khẩu)
          // Không gọi refresh cho chính login/refresh
        }
        return throwError(() => err);
      })
    );
  }

  // Request đã được retry một lần rồi mà vẫn 401 → không retry nữa, logout và throw
  if (req.headers.has(SKIP_AUTH_RETRY_HEADER)) {
    auth.logout();
    return next(addBearerToken(req, auth.getAccessToken())).pipe(
      catchError((err) => throwError(() => err))
    );
  }

  const reqWithToken = addBearerToken(req, auth.getAccessToken());

  return next(reqWithToken).pipe(
    catchError((err: unknown) => {
      const httpErr = err instanceof HttpErrorResponse ? err : null;
      if (!httpErr || httpErr.status !== 401) {
        return throwError(() => err);
      }

      // Đang có refresh chạy rồi → đợi refresh xong rồi retry
      const refreshInProgress = auth.getRefreshInProgress$().getValue();
      if (refreshInProgress) {
        return refreshInProgress.pipe(
          switchMap(() => {
            const retryReq = addBearerToken(req, auth.getAccessToken()).clone({
              setHeaders: { [SKIP_AUTH_RETRY_HEADER]: '1' }
            });
            return next(retryReq);
          }),
          catchError((e) => throwError(() => e))
        );
      }

      // Chưa có refresh → gọi refresh một lần, các request 401 khác sẽ dùng chung qua getValue()
      const refresh$ = auth.refreshToken();
      auth.setRefreshInProgress(refresh$);

      return refresh$.pipe(
        switchMap(() => {
          const retryReq = addBearerToken(req, auth.getAccessToken()).clone({
            setHeaders: { [SKIP_AUTH_RETRY_HEADER]: '1' }
          });
          return next(retryReq);
        }),
        tap(() => auth.setRefreshInProgress(null)),
        catchError((e) => {
          auth.setRefreshInProgress(null);
          return throwError(() => e);
        })
      );
    })
  );
};
