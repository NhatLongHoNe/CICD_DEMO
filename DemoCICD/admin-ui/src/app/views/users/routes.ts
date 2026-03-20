import { Routes } from '@angular/router';
import { permissionGuard, USER_CREATE, USER_UPDATE, USER_VIEW } from '../../core/auth';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./user-list/user-list.component').then(m => m.UserListComponent),
    data: { title: $localize`Users`, permission: USER_VIEW },
    canActivate: [permissionGuard(USER_VIEW)]
  },
  {
    path: 'new',
    loadComponent: () => import('./user-form/user-form.component').then(m => m.UserFormComponent),
    data: { title: $localize`New User`, permission: USER_CREATE },
    canActivate: [permissionGuard(USER_CREATE)]
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./user-form/user-form.component').then(m => m.UserFormComponent),
    data: { title: $localize`Edit User`, permission: USER_UPDATE },
    canActivate: [permissionGuard(USER_UPDATE)]
  }
];
