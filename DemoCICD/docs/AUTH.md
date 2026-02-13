# Tài liệu Authentication – Angular Admin UI

Tài liệu mô tả luồng xác thực (JWT + refresh token) dùng trong admin-ui, tích hợp với .NET Web API.

---

## 1. API Backend (.NET)

### Endpoints

| Method | URL | Mô tả |
|--------|-----|--------|
| POST | `/api/v1/auth/login` | Đăng nhập, trả về access + refresh token |
| POST | `/api/v1/auth/refresh-token` | Đổi refresh token lấy token mới |

### Response login / refresh-token

```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "2026-02-13T12:00:00Z"
}
```

- `expiresAt`: thời điểm hết hạn access token (UTC, ISO 8601).

### Body refresh-token

```json
{
  "refreshToken": "string"
}
```

### Lỗi (ví dụ sai mật khẩu)

```json
{
  "type": "Auth.InvalidCredentials",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid username or password.",
  "errors": null
}
```

UI ưu tiên hiển thị `detail`, rồi `title`, rồi `message`.

---

## 2. Cấu trúc thư mục Auth

```
admin-ui/src/app/core/auth/
├── auth.models.ts    # Interface: AuthTokenResponse, LoginRequest, RefreshTokenRequest
├── auth.service.ts  # Service: login, logout, refreshToken, isLoggedIn, ...
├── auth.interceptor.ts  # HTTP interceptor: Bearer token, 401 → refresh → retry
├── auth.guard.ts    # Route guard: yêu cầu đăng nhập
└── index.ts         # Barrel export
```

---

## 3. Token storage

| Token | Nơi lưu | Ghi chú |
|-------|---------|--------|
| **Access token** | Chỉ trong **memory** (biến trong `AuthService`) | Không lưu localStorage/sessionStorage |
| **Refresh token** | **localStorage** (key: `refresh_token`) | Dùng để lấy access token mới khi hết hạn / reload trang |

### Vì sao không lưu access token vào localStorage?

- **XSS:** Script độc có thể đọc `localStorage` và đánh cắp access token, dùng để gọi API thay user.
- Access token trong **memory**: đóng tab hoặc reload thì mất, giảm rủi ro. Kẻ tấn công phải chạy script trong đúng tab/session.
- **Refresh token** trong localStorage chấp nhận được vì:
  - Chỉ dùng để gọi `/auth/refresh-token`, không gửi kèm mọi request.
  - Backend nên revoke refresh token khi đổi mật khẩu / logout toàn bộ thiết bị.

---

## 4. AuthService (`auth.service.ts`)

### Cấu hình

- **Base URL:** `environment.apiBaseUrl + '/api/v1'` (ví dụ: `http://localhost:5207/api/v1`).
- **Proactive refresh:** Nếu access token còn **&lt; 60 giây** thì tự gọi refresh (timer kiểm tra mỗi 30s).

### API public

| Method | Kiểu trả về | Mô tả |
|--------|-------------|--------|
| `login(credentials)` | `Observable<AuthTokenResponse>` | Gọi API login, lưu token, bật proactive refresh |
| `logout()` | `void` | Xóa token (memory + localStorage), clear timer, redirect `/login` |
| `refreshToken()` | `Observable<boolean>` | Gọi API refresh, cập nhật access + expiresAt; lỗi thì logout |
| `isLoggedIn()` | `boolean` | Có access token trong memory hoặc có refresh token trong localStorage |
| `hasAccessToken()` | `boolean` | Có access token trong memory (sẵn sàng gửi kèm request) |
| `getAccessToken()` | `string \| null` | Trả về access token hiện tại (cho interceptor) |
| `restoreSession()` | `Observable<boolean>` | Nếu chỉ có refresh token (sau reload), gọi refresh để lấy lại access token |
| `isAccessTokenExpiringSoon(seconds?)` | `boolean` | Token sắp hết hạn trong `seconds` giây (mặc định 60) |

### Đồng bộ refresh (tránh nhiều request refresh song song)

- `BehaviorSubject` `refreshInProgress$` lưu Observable của lần refresh đang chạy.
- Interceptor: gặp 401 thì kiểm tra đã có `refreshInProgress$` chưa; nếu có thì đợi, nếu chưa thì gọi `refreshToken()` và set vào `refreshInProgress$`. Nhiều request 401 cùng lúc chỉ tạo **một** lần gọi refresh.

---

## 5. HTTP Interceptor (`auth.interceptor.ts`)

### Chức năng

1. **Gắn Bearer token**  
   Với mọi request **không** phải `/auth/login` và `/auth/refresh-token`: thêm header  
   `Authorization: Bearer <accessToken>`.

2. **Request login / refresh-token**  
   Không gắn token, không xử lý 401 (để component/login API tự xử lý).

3. **401 với request thường**  
   - Nếu đang có một lần refresh chạy → đợi refresh xong → **retry 1 lần** request với token mới.  
   - Nếu chưa có → gọi `refreshToken()` → đợi xong → retry 1 lần.  
   - Request retry được đánh dấu bằng header `X-Skip-Auth-Retry` để **không** retry lần nữa (tránh loop).

4. **Refresh thất bại**  
   `AuthService.logout()` → redirect `/login`.

5. **Đã retry 1 lần mà vẫn 401**  
   Logout và không retry nữa.

---

## 6. Auth Guard (`auth.guard.ts`)

- **Tên:** `authGuard` (functional guard, Angular 17+).
- **Logic:**
  - Đã có access token (`hasAccessToken()`) → cho vào route.
  - Chưa có → gọi `restoreSession()` (refresh từ localStorage).
  - `restoreSession()` thành công → cho vào.
  - Thất bại → redirect `/login` (`router.createUrlTree(['/login'])`).

### Gắn vào route

Trong `app.routes.ts`, route layout chính (dashboard, pages, ...) dùng:

```ts
canActivate: [authGuard]
```

---

## 7. Environment

- **Development:** `src/environments/environment.ts`  
  - `apiBaseUrl`: `http://localhost:5207`
- **Production:** `src/environments/environment.production.ts` (dùng khi build production)  
  - `apiBaseUrl`: `https://api.myapp.com` (hoặc URL thật của API)

Build production dùng `fileReplacements` trong `angular.json` để thay `environment.ts` bằng `environment.production.ts`.

---

## 8. Cấu hình ứng dụng

### `app.config.ts`

- `provideHttpClient(withInterceptors([authInterceptor]))`  
  → Mọi HTTP request đi qua `authInterceptor`.

### Login component

- Form reactive (username, password).
- Submit → `AuthService.login({ username, password })`.
- Thành công → `router.navigate(['/dashboard'])`.
- Lỗi → hiển thị `detail` (hoặc `title` / `message`) từ response.

---

## 9. Luồng tổng quát

1. **Đăng nhập:** User nhập username/password → POST `/api/v1/auth/login` → lưu access token (memory), refresh token (localStorage), `expiresAt` → bật proactive refresh.
2. **Request API:** Interceptor gắn `Authorization: Bearer <accessToken>`.
3. **401:** Interceptor gọi refresh (một lần chung cho nhiều request) → retry request với token mới; refresh fail → logout, redirect `/login`.
4. **Proactive refresh:** Timer kiểm tra định kỳ; nếu access token còn &lt; 60s → gọi refresh.
5. **Reload trang:** Chỉ còn refresh token (localStorage). Vào route có guard → guard gọi `restoreSession()` (refresh) → có access token → vào được app.
6. **Logout:** Xóa token, clear timer, redirect `/login`.

---

## 10. CORS (Backend .NET)

Angular chạy ở origin khác (ví dụ `http://localhost:4200`), API ở `http://localhost:5207` → cần bật CORS trên API.

Trong `Program.cs`:

- `AddCors`: policy `WithOrigins` từ config (ví dụ `Cors:AllowedOrigins`), `AllowAnyHeader()`, `AllowAnyMethod()`.
- `UseCors()` gọi **trước** `UseAuthentication()`.

Ví dụ trong `appsettings.Development.json`:

```json
"Cors": {
  "AllowedOrigins": ["http://localhost:4200"]
}
```

---

*Tài liệu cập nhật theo implementation hiện tại trong admin-ui (Angular 17+ standalone).*
