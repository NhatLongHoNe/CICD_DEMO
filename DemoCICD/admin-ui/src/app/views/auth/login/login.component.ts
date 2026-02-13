import { ChangeDetectorRef, Component } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
  loginForm = this.fb.group({
    username: [''],
    password: ['']
  });
  loading = false;
  errorMessage: string | null = null;

  constructor(
    private readonly fb: NonNullableFormBuilder,
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  onSubmit(): void {
    this.errorMessage = null;
    const { username, password } = this.loginForm.getRawValue();
    if (!username.trim() || !password) {
      this.errorMessage = 'Vui lòng nhập username và password.';
      return;
    }
    this.loading = true;
    this.loginForm.disable();
    this.cdr.markForCheck();
    this.auth.login({ username: username.trim(), password }).subscribe({
      next: () => {
        this.loading = false;
        this.loginForm.enable();
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        this.loginForm.enable();
        // API trả 400 với body: { type, title, status, detail, errors }
        const body = err?.error;
        this.errorMessage =
          body?.detail ?? body?.title ?? body?.message ?? err?.message ?? 'Đăng nhập thất bại.';
        this.cdr.markForCheck();
      }
    });
  }
}
