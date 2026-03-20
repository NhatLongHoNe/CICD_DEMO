import { Component, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  ButtonDirective,
  CardBodyComponent,
  CardComponent,
  CardHeaderComponent,
  ColComponent,
  RowComponent,
  TableDirective
} from '@coreui/angular';
import { IconDirective } from '@coreui/icons-angular';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { UserService, type UserResponse, type PagedResult } from '../../../core/services/user.service';
import { AuthService, USER_CREATE, USER_DELETE, USER_UPDATE } from '../../../core/auth';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  imports: [
    CardComponent,
    CardHeaderComponent,
    CardBodyComponent,
    RowComponent,
    ColComponent,
    TableDirective,
    ButtonDirective,
    IconDirective,
    RouterLink,
    FormsModule
  ]
})
export class UserListComponent {
  private readonly userService = inject(UserService);
  private readonly auth = inject(AuthService);
  private readonly toastr = inject(ToastrService);

  searchTerm = '';
  pageIndex = 1;
  pageSize = 10;
  loading = signal(false);
  pagedResult = signal<PagedResult<UserResponse> | null>(null);

  users = computed(() => this.pagedResult()?.items ?? []);
  totalCount = computed(() => this.pagedResult()?.totalCount ?? 0);
  canCreate = () => this.auth.hasPermission(USER_CREATE);
  canUpdate = () => this.auth.hasPermission(USER_UPDATE);
  canDelete = () => this.auth.hasPermission(USER_DELETE);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.userService.getList(this.searchTerm || undefined, this.pageIndex, this.pageSize).subscribe({
      next: (res: PagedResult<UserResponse>) => {
        this.pagedResult.set(res);
        this.loading.set(false);
      },
      error: (err: { error?: { detail?: string }; message?: string }) => {
        this.toastr.error(err?.error?.detail ?? err?.message ?? 'Load failed');
        this.loading.set(false);
      }
    });
  }

  search(): void {
    this.pageIndex = 1;
    this.load();
  }

  pagePrev(): void {
    if (this.pageIndex > 1) {
      this.pageIndex--;
      this.load();
    }
  }

  pageNext(): void {
    const pr = this.pagedResult();
    if (pr && pr.pageIndex * pr.pageSize < pr.totalCount) {
      this.pageIndex++;
      this.load();
    }
  }

  deleteUser(user: UserResponse): void {
    if (!confirm(`Xóa user "${user.userName}"?`)) return;
    this.userService.delete(user.id).subscribe({
      next: () => {
        this.toastr.success('Đã xóa user.');
        this.load();
      },
      error: (err: { error?: { detail?: string } }) => this.toastr.error(err?.error?.detail ?? 'Delete failed')
    });
  }
}
