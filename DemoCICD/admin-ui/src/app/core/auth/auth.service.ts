import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, map, shareReplay, switchMap, tap } from 'rxjs/operators';

import type { AuthTokenResponse, LoginRequest } from './auth.models';
import { environment } from '../../../environments/environment';

/** Key lưu refreshToken trong localStorage (không lưu accessToken - xem comment cuối file) */
const REFRESH_TOKEN_KEY = 'refresh_token';

/** Path API auth (versioned), ví dụ: /api/v1 */
const API_V1 = '/api/v1';

/** Số giây trước khi hết hạn thì tự động gọi refresh (proactive refresh) */
const PROACTIVE_REFRESH_SECONDS = 60;

@Injectable({ providedIn: 'root' })
export class AuthService {
  /** Base URL API = environment.apiBaseUrl + API_V1, ví dụ: http://localhost:5207/api/v1 */
  private readonly apiBase = `${environment.apiBaseUrl}${API_V1}`;

  /**
   * Access token chỉ lưu trong memory → khi refresh trang sẽ mất.
   * Session được phục hồi bằng refreshToken (localStorage) khi gọi refreshToken().
   */
  private accessToken: string | null = null;

  /**
   * Thời điểm hết hạn access token (UTC ms).
   * Dùng cho proactive refresh: nếu còn < PROACTIVE_REFRESH_SECONDS thì refresh.
   */
  private expiresAt: number | null = null;

  /**
   * BehaviorSubject dùng để đồng bộ refresh: nhiều request 401 cùng lúc chỉ trigger 1 lần refresh.
   * Giá trị là Observable refresh đang chạy (hoặc null nếu không có).
   */
  private refreshInProgress$ = new BehaviorSubject<Observable<boolean> | null>(null);

  /** Timer proactive refresh - clear khi logout để tránh leak */
  private proactiveRefreshTimer: ReturnType<typeof setInterval> | null = null;

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  /**
   * Đăng nhập: gọi API, lưu token đúng strategy, bật proactive refresh.
   */
  login(credentials: LoginRequest): Observable<AuthTokenResponse> {
    return this.http
      .post<AuthTokenResponse>(`${this.apiBase}/auth/login`, credentials)
      .pipe(
        tap((res) => this.handleTokenResponse(res)),
        tap(() => this.scheduleProactiveRefresh())
      );
  }

  /**
   * Đăng xuất: xóa token memory + localStorage, clear timer, redirect.
   */
  logout(): void {
    this.clearTokens();
    this.clearProactiveRefreshTimer();
    this.router.navigate(['/login'], { queryParamsHandling: 'merge' });
  }

  /**
   * Refresh token: dùng refreshToken trong localStorage, cập nhật accessToken + expiresAt.
   * Dùng chung 1 Observable khi nhiều subscriber (shareReplay) để không gọi API nhiều lần.
   */
  refreshToken(): Observable<boolean> {
    const stored = this.getStoredRefreshToken();
    if (!stored) {
      this.logout();
      return throwError(() => new Error('No refresh token'));
    }

    const refresh$ = this.http
      .post<AuthTokenResponse>(`${this.apiBase}/auth/refresh-token`, { refreshToken: stored })
      .pipe(
        tap((res) => this.handleTokenResponse(res)),
        tap(() => this.scheduleProactiveRefresh()),
        map(() => true),
        shareReplay({ bufferSize: 1, refCount: true }),
        catchError((err) => {
          this.logout();
          return throwError(() => err);
        })
      );

    return refresh$;
  }

  /**
   * Trả về Observable refresh đang chạy, hoặc null.
   * Interceptor dùng để queue: nếu đang refresh thì đợi, không thì gọi refreshToken() và set vào đây.
   */
  getRefreshInProgress$(): BehaviorSubject<Observable<boolean> | null> {
    return this.refreshInProgress$;
  }

  /**
   * Set observable refresh đang chạy (interceptor gọi khi bắt đầu refresh).
   */
  setRefreshInProgress(obs: Observable<boolean> | null): void {
    this.refreshInProgress$.next(obs);
  }

  /**
   * Khôi phục session sau reload: nếu có refreshToken nhưng chưa có accessToken thì gọi refresh.
   * Có thể gọi từ APP_INITIALIZER hoặc guard/layout để sẵn sàng token trước khi gửi request.
   */
  restoreSession(): Observable<boolean> {
    if (this.accessToken !== null) return of(true);
    const stored = this.getStoredRefreshToken();
    if (!stored) return of(false);
    return this.refreshToken().pipe(
      map(() => true),
      catchError(() => of(false))
    );
  }

  /** Đã đăng nhập khi có accessToken trong memory (hoặc có refreshToken để restore). */
  isLoggedIn(): boolean {
    return this.accessToken !== null || this.getStoredRefreshToken() !== null;
  }

  /** Có accessToken trong memory (sẵn sàng gửi kèm request). */
  hasAccessToken(): boolean {
    return this.accessToken !== null && this.accessToken.length > 0;
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  /**
   * Lấy danh sách permission từ JWT (claim type "permission").
   * Dùng cho permission guard và ẩn/hiện menu, nút theo quyền.
   */
  getPermissions(): string[] {
    const token = this.accessToken;
    if (!token) return [];
    try {
      const payload = token.split('.')[1];
      if (!payload) return [];
      const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
      const claims = decoded as Record<string, unknown>;
      // Backend gửi permission claims với type "permission"
      const perm = claims['permission'];
      if (Array.isArray(perm)) return perm as string[];
      if (typeof perm === 'string') return [perm];
      // Hoặc có thể nằm trong một mảng theo key khác
      const arr = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/permission'];
      if (Array.isArray(arr)) return arr as string[];
      return [];
    } catch {
      return [];
    }
  }

  hasPermission(permission: string): boolean {
    return this.getPermissions().some(p => p === permission);
  }

  /**
   * Kiểm tra access token sắp hết hạn (còn ít hơn N giây).
   * Dùng cho interceptor hoặc proactive refresh.
   */
  isAccessTokenExpiringSoon(withinSeconds: number = PROACTIVE_REFRESH_SECONDS): boolean {
    if (this.expiresAt == null) return true;
    const now = Date.now();
    return this.expiresAt - now < withinSeconds * 1000;
  }

  // ---------- Private ----------

  private handleTokenResponse(res: AuthTokenResponse): void {
    this.accessToken = res.accessToken;
    this.expiresAt = new Date(res.expiresAt).getTime();
    if (res.refreshToken) {
      try {
        localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
      } catch {
        // localStorage full hoặc private mode
      }
    }
  }

  private clearTokens(): void {
    this.accessToken = null;
    this.expiresAt = null;
    try {
      localStorage.removeItem(REFRESH_TOKEN_KEY);
    } catch {
      // ignore
    }
  }

  private getStoredRefreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
      return null;
    }
  }

  private clearProactiveRefreshTimer(): void {
    if (this.proactiveRefreshTimer != null) {
      clearInterval(this.proactiveRefreshTimer);
      this.proactiveRefreshTimer = null;
    }
  }

  /**
   * Proactive refresh: đặt timer kiểm tra định kỳ, nếu còn < 60s thì gọi refresh.
   */
  private scheduleProactiveRefresh(): void {
    this.clearProactiveRefreshTimer();
    if (!this.expiresAt) return;

    const check = () => {
      if (!this.isLoggedIn() || !this.getStoredRefreshToken()) {
        this.clearProactiveRefreshTimer();
        return;
      }
      if (this.isAccessTokenExpiringSoon(PROACTIVE_REFRESH_SECONDS)) {
        this.clearProactiveRefreshTimer();
        this.refreshToken().subscribe({
          error: () => {
            // logout đã được gọi trong refreshToken catchError
          }
        });
      }
    };

    // Kiểm tra mỗi 30 giây
    this.proactiveRefreshTimer = setInterval(check, 30_000);
    check();
  }
}

/*
 * TẠI SAO KHÔNG LƯU ACCESS TOKEN VÀO LOCALSTORAGE?
 *
 * - XSS: Script độc hại có thể đọc localStorage và lấy accessToken, gửi sang server khác.
 *   Access token thường có quyền cao, thời hạn ngắn nhưng vẫn đủ để tấn công.
 * - Access token để trong memory: khi đóng tab hoặc refresh thì mất, giảm rủi ro.
 *   Kẻ tấn công cần chạy script trong cùng tab/session mới đọc được.
 * - Refresh token trong localStorage chấp nhận được vì:
 *   + Backend nên revoke refresh token khi đổi mật khẩu / logout toàn bộ thiết bị.
 *   + Refresh token chỉ dùng để lấy access token mới, không gửi kèm mọi request.
 * - Best practice: access token trong memory (hoặc httpOnly cookie nếu dùng cookie),
 *   refresh token có thể localStorage hoặc httpOnly cookie tùy backend.
 */
