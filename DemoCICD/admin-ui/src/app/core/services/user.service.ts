import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

const API = `${environment.apiBaseUrl}/api/v1/users`;

/** Khớp với API JSON (camelCase) */
export interface UserResponse {
  id: string;
  userName: string;
  email: string | null;
  fullName: string | null;
  emailConfirmed: boolean;
  lockoutEnabled: boolean;
  lockoutEnd: string | null;
  roles: string[];
}

export interface RoleItemResponse {
  id: string;
  name: string;
  description: string | null;
  roleCode: string | null;
}

export interface FunctionActionNodeResponse {
  functionId: string;
  functionName: string;
  actions: { actionId: string; actionName: string }[];
}

/** Khớp với API JSON (camelCase) */
export interface PagedResult<T> {
  items: T[];
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  password: string;
  fullName?: string;
  roleIds: string[];
}

export interface UpdateUserRequest {
  email?: string;
  fullName?: string;
  newPassword?: string;
  roleIds: string[];
}

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private readonly http: HttpClient) {}

  getList(searchTerm?: string, pageIndex = 1, pageSize = 10): Observable<PagedResult<UserResponse>> {
    let params = new HttpParams().set('pageIndex', String(pageIndex)).set('pageSize', String(pageSize));
    if (searchTerm?.trim()) {
      params = params.set('searchTerm', searchTerm.trim());
    }
    return this.http.get<PagedResult<UserResponse>>(API, { params });
  }

  getById(id: string): Observable<UserResponse | null> {
    return this.http.get<UserResponse | null>(`${API}/${id}`);
  }

  getRoles(): Observable<RoleItemResponse[]> {
    return this.http.get<RoleItemResponse[]>(`${API}/roles`);
  }

  getFunctionsActions(): Observable<FunctionActionNodeResponse[]> {
    return this.http.get<FunctionActionNodeResponse[]>(`${API}/functions-actions`);
  }

  create(body: CreateUserRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(API, {
      userName: body.userName,
      email: body.email,
      password: body.password,
      fullName: body.fullName ?? null,
      roleIds: body.roleIds
    });
  }

  update(id: string, body: UpdateUserRequest): Observable<void> {
    return this.http.put<void>(`${API}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${API}/${id}`);
  }
}
