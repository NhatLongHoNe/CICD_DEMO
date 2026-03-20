import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Permission constant khớp backend */
export const USER_VIEW = 'USER.VIEW';
export const USER_CREATE = 'USER.CREATE';
export const USER_UPDATE = 'USER.UPDATE';
export const USER_DELETE = 'USER.DELETE';

/**
 * Guard kiểm tra permission (từ JWT). Dùng trong route data: data: { permission: 'USER.VIEW' }.
 */
export function permissionGuard(permission: string): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (auth.hasPermission(permission)) return true;
    router.navigate(['/dashboard']);
    return false;
  };
}

/** Guard đọc permission từ route.data['permission'] */
export const permissionFromRouteGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const permission = (route.data && route.data['permission']) as string | undefined;
  if (!permission) return true;
  return inject(AuthService).hasPermission(permission) ? true : inject(Router).createUrlTree(['/dashboard']);
};
