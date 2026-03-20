import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { IconDirective } from '@coreui/icons-angular';
import {
  ButtonDirective,
  CardBodyComponent,
  CardComponent,
  CardGroupComponent,
  ColComponent,
  ContainerComponent,
  FormControlDirective,
  FormDirective,
  InputGroupComponent,
  InputGroupTextDirective,
  RowComponent
} from '@coreui/angular';

import { AuthService } from '../../../core/auth';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  imports: [
    ReactiveFormsModule,
    ContainerComponent,
    RowComponent,
    ColComponent,
    CardGroupComponent,
    CardComponent,
    CardBodyComponent,
    FormDirective,
    InputGroupComponent,
    InputGroupTextDirective,
    IconDirective,
    FormControlDirective,
    ButtonDirective
  ]
})
export class LoginComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);

  loginForm = this.fb.group({
    username: [''],
    password: ['']
  });
  loading = false;

  onSubmit(): void {
    const { username, password } = this.loginForm.getRawValue();
    if (!username.trim() || !password) {
      this.toastr.error('Vui lòng nhập username và password.');
      return;
    }
    this.loading = true;
    this.loginForm.disable();
    this.auth.login({ username: username.trim(), password }).subscribe({
      next: () => {
        this.loading = false;
        this.loginForm.enable();
        this.toastr.success('Đăng nhập thành công.');
        this.router.navigate(['/dashboard']);
      },
      error: (err: { error?: { detail?: string; title?: string; message?: string }; message?: string }) => {
        this.loading = false;
        this.loginForm.enable();
        const body = err?.error;
        const msg = body?.detail ?? body?.title ?? body?.message ?? err?.message ?? 'Đăng nhập thất bại.';
        this.toastr.error(msg);
      }
    });
  }
}
