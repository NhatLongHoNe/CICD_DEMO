# JWT Authentication - Kiến trúc & Triển khai

## 1. Luồng Authentication End-to-End

```
┌─────────────┐     POST /api/v1/auth/login      ┌──────────────┐
│   Client    │ ──────────────────────────────►  │  AuthApi     │
│             │     { userName, password }       │  (Presentation)│
└─────────────┘                                  └──────┬───────┘
       ▲                                                │ sender.Send(LoginCommand)
       │                                                ▼
       │                                        ┌──────────────┐
       │                                        │ LoginCommand │
       │                                        │ Handler      │
       │                                        │ (Application)│
       │                                        └──────┬───────┘
       │                                               │
       │     ┌─────────────────────────────────────────┼─────────────────────────────────┐
       │     │ 1. UserManager.FindByNameAsync + CheckPasswordAsync                       │
       │     │ 2. IAccessTokenService.GenerateAccessToken(userId, userName, roles)       │
       │     │ 3. GenerateRefreshToken (RandomNumberGenerator) → IRefreshTokenRepository │
       │     │ 4. Return TokenResponse                                                   │
       │     └─────────────────────────────────────────┼─────────────────────────────────┘
       │                                                │
       │     { accessToken, refreshToken, expiresAt }   │
       ◄────────────────────────────────────────────────┘
```

### Refresh Token Flow

```
Client gửi refreshToken cũ → Handler validate → Revoke token cũ → Tạo cặp token mới → Return
```

---

## 2. Cấu trúc Folder / Class

```
Contract/
├── Services/V1/Auth/
│   ├── Command.cs          # LoginCommand, RefreshTokenCommand
│   ├── Response.cs         # TokenResponse
│   └── Validators/
│       ├── LoginCommandValidator.cs
│       └── RefreshTokenCommandValidator.cs

Application/
├── Abstractions/
│   └── IAccessTokenService.cs     # Interface - Dependency Inversion
├── UserCases/V1/Commands/Auth/
│   ├── LoginCommandHandler.cs
│   └── RefreshTokenCommandHandler.cs

Infrastructure/
├── Authentication/
│   ├── JwtOptions.cs
│   └── JwtTokenService.cs         # Implementation của IAccessTokenService
└── DependencyInjection/
    └── Extensions/ServiceCollectionExtensions.cs

Domain/
├── Entities/Identity/
│   └── RefreshToken.cs
└── Abstractions/Repositories/
    └── IRefreshTokenRepository.cs

Persistence/
├── Configurations/RefreshTokenConfiguration.cs
├── Repositories/RefreshTokenRepository.cs
└── Migrations/...AddRefreshToken

Presentation/
└── APIs/Auth/
    └── AuthApi.cs                 # Minimal API endpoints
```

---

## 3. Trade-offs Kiến trúc

### 3.1. Vì sao Identity nằm ở Persistence?

- **ASP.NET Core Identity** là infrastructure concern: hash password, claim types, token storage, DbContext mapping.
- **Domain** chỉ định nghĩa `AppUser`, `AppRole` (entities) – có thể tranh luận đưa vào Domain vì chúng là core business concepts.
- **Persistence** đăng ký `AddIdentityCore`, `AddEntityFrameworkStores` – nơi gắn Identity với DbContext.
- **Kết luận**: Entity trong Domain; registration và store trong Persistence. Domain không biết EF hay Identity implementation.

### 3.2. IAccessTokenService trong Application, implementation trong Infrastructure

- **DIP**: Application (high-level) không phụ thuộc chi tiết JWT.
- Application chỉ cần: "generate token cho userId + roles".
- Infrastructure chọn thuật toán, library, format (JWT).
- Dễ test: mock `IAccessTokenService` trong unit test.

### 3.3. RefreshToken – Entity trong Domain, Repository trong Persistence

- RefreshToken là domain concept: "phiên làm việc có thể thu hồi".
- Domain định nghĩa `IRefreshTokenRepository` (interface).
- Persistence implement, lưu vào SQL.
- Cho phép rotate token, revoke, kiểm tra theo thiết kế domain.

### 3.4. TransactionPipelineBehavior và Auth Commands

- Login/RefreshToken là Commands (kết thúc bằng `Command`).
- Behavior wrap trong transaction.
- RefreshTokenHandler: Revoke token cũ + Add token mới trong cùng transaction → atomic.

---

## 4. Sử dụng

### Endpoints

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| POST | `/api/v1/auth/login` | `{ "userName": "admin", "password": "xxx" }` | `{ accessToken, refreshToken, expiresAt }` |
| POST | `/api/v1/auth/refresh` | `{ "refreshToken": "..." }` | `{ accessToken, refreshToken, expiresAt }` |

### Bảo vệ endpoint

```csharp
group.MapGet(string.Empty, GetProducts).RequireAuthorization();
// hoặc
group.MapGet("{id}", GetById).RequireAuthorization(policy: "AdminOnly");
```

### Cấu hình JWT (appsettings / env)

- `Jwt:Secret`: tối thiểu 32 ký tự (HS256).
- Production: dùng User Secrets / Azure Key Vault / env var.

---

## 5. Tạo user test

```powershell
# Trong Package Manager Console hoặc tạo endpoint seed
# Hoặc dùng SQL:
# INSERT vào AspNetUsers (cần hash password từ UserManager)
```

Gợi ý: thêm endpoint `/api/v1/auth/register` (dev only) hoặc seed data trong migration.
