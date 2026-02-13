import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { AuthService } from './auth.service';

/**
 * Guard bảo vệ route: đã đăng nhập (có accessToken hoặc restore được bằng refreshToken) mới vào.
 * Nếu chưa đăng nhập → redirect về /login.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.hasAccessToken()) {
    return true;
  }

  return auth.restoreSession().pipe(
    map((ok) => (ok ? true : router.createUrlTree(['/login'])))
  );
};
