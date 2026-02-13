/**
 * Environment - production
 * Được thay thế khi build với configuration production (fileReplacements trong angular.json)
 */
export const environment = {
  production: true,
  /** Base URL backend API (không có trailing slash). Ví dụ: https://api.myapp.com */
  apiBaseUrl: 'https://api.myapp.com'
} as const;
