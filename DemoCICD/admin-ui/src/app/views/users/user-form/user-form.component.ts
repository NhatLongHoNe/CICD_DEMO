import { Component, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ButtonDirective,
  CardBodyComponent,
  CardComponent,
  CardHeaderComponent,
  ColComponent,
  RowComponent
} from '@coreui/angular';
import { ToastrService } from 'ngx-toastr';
import { UserService, type RoleItemResponse, type UserResponse } from '../../../core/services/user.service';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  imports: [
    ReactiveFormsModule,
    CardComponent,
    CardHeaderComponent,
    CardBodyComponent,
    RowComponent,
    ColComponent,
    ButtonDirective,
    RouterLink
  ]
})
export class UserFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(UserService);
  private readonly toastr = inject(ToastrService);

  isEdit = false;
  userId: string | null = null;
  roles = signal<RoleItemResponse[]>([]);
  loading = signal(false);

  form = this.fb.group({
    userName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.email]],
    password: ['', []],
    fullName: [''],
    roleIds: this.fb.array<string>([])
  });

  ngOnInit(): void {
    this.userService.getRoles().subscribe({
      next: (list: RoleItemResponse[]) => this.roles.set(list),
      error: () => this.toastr.error('Failed to load roles')
    });
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.userId = id;
      this.isEdit = true;
      this.form.controls.userName.disable();
      this.form.controls.password.clearValidators();
      this.userService.getById(id).subscribe({
        next: (user: UserResponse | null) => {
          if (user) {
            this.form.patchValue({
              userName: user.userName,
              email: user.email ?? '',
              fullName: user.fullName ?? ''
            });
            const roleIdsCtrl = this.form.controls.roleIds;
            roleIdsCtrl.clear();
            const roleList = this.roles();
            roleList.forEach((r) => {
              if (user.roles.includes(r.name)) {
                roleIdsCtrl.push(this.fb.control(r.id));
              }
            });
          }
        },
        error: (err: { error?: { detail?: string } }) => this.toastr.error(err?.error?.detail ?? 'Load user failed')
      });
    } else {
      this.form.controls.password.setValidators([Validators.required, Validators.minLength(6)]);
    }
  }

  get roleIdsControl() {
    return this.form.get('roleIds');
  }

  toggleRole(roleId: string, checked: boolean): void {
    const arr = this.form.controls.roleIds;
    if (checked) {
      if (!arr.value.includes(roleId)) arr.push(this.fb.control(roleId));
    } else {
      const i = arr.value.indexOf(roleId);
      if (i >= 0) arr.removeAt(i);
    }
  }

  hasRole(roleId: string): boolean {
    return this.form.controls.roleIds.value.includes(roleId);
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.loading.set(true);
    const v = this.form.getRawValue();
    if (this.isEdit && this.userId) {
      this.userService
        .update(this.userId, {
          email: v.email || undefined,
          fullName: v.fullName || undefined,
          newPassword: v.password || undefined,
          roleIds: v.roleIds
        })
        .subscribe({
          next: () => {
            this.toastr.success('Đã cập nhật user.');
            this.router.navigate(['/users']);
          },
          error: (err: { error?: { detail?: string } }) => {
            this.toastr.error(err?.error?.detail ?? 'Update failed');
            this.loading.set(false);
          }
        });
    } else {
      this.userService
        .create({
          userName: v.userName,
          email: v.email || '',
          password: v.password,
          fullName: v.fullName || undefined,
          roleIds: v.roleIds
        })
        .subscribe({
          next: () => {
            this.toastr.success('Đã tạo user.');
            this.router.navigate(['/users']);
          },
          error: (err: { error?: { detail?: string } }) => {
            this.toastr.error(err?.error?.detail ?? 'Create failed');
            this.loading.set(false);
          }
        });
    }
  }
}
