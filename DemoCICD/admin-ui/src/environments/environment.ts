/**
 * Environment - development
 * Dùng khi chạy ng serve hoặc build với configuration development
 */
export const environment = {
  production: false,
  /** Base URL backend API (không có trailing slash). Ví dụ: http://localhost:5207 */
  apiBaseUrl: 'http://localhost:5207'
} as const;
