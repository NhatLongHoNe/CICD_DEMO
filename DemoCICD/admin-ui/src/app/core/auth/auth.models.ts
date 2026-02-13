/**
 * Auth API models - khớp với .NET Web API response
 */

/** Response từ POST /api/auth/login và POST /api/auth/refresh-token */
export interface AuthTokenResponse {
  accessToken: string;
  refreshToken: string;
  /** UTC datetime string (ISO 8601), ví dụ: "2026-02-13T12:00:00Z" */
  expiresAt: string;
}

/** Body gửi lên POST /api/auth/refresh-token */
export interface RefreshTokenRequest {
  refreshToken: string;
}

/** Body gửi lên POST /api/auth/login (tùy backend của bạn) */
export interface LoginRequest {
  username?: string;
  password?: string;
  email?: string;
  [key: string]: unknown;
}
