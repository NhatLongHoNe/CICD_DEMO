# Hệ thống JWT Authentication & Permission Authorization

## Tổng quan

Hệ thống kiểm tra đăng nhập và kiểm tra quyền trong ASP.NET Core sử dụng JWT, theo Clean Architecture.

- **API bắt buộc đăng nhập**
- **Product API** phân quyền: View, Create, Update, Delete
- Dùng **custom AuthorizationHandler**
- Permission lấy từ **claim trong JWT** (không query DB mỗi request)
- **401** nếu chưa login, **403** nếu thiếu quyền

---

## Kiến trúc (Clean Architecture)

| Layer | Thành phần |
|-------|------------|
| **Domain** | `IPermissionRepository` (abstraction) |
| **Application** | `IAccessTokenService`, `LoginCommandHandler`, `RefreshTokenCommandHandler` |
| **Infrastructure** | `JwtTokenService`, `PermissionAuthorizationHandler`, `AddPermissionAuthorization` |
| **Persistence** | `PermissionRepository`, `IPermissionRepository` registration |
| **Presentation** | `ProductApi`, `ProductCarterApi`, `ProductsController`, `RequirePermissionAttribute` |

---

## Chi tiết triển khai

### 1. JWT + Permission Claims (Infrastructure)

**File:** `DemoCICD.Application/Abstractions/IAccessTokenService.cs`

```csharp
string GenerateAccessToken(
    Guid userId,
    string userName,
    IReadOnlyList<string> roles,
    IReadOnlyList<string> permissions);
```

**File:** `DemoCICD.Infrastructure/Authentication/JwtTokenService.cs`

- Thêm claims `permission` (claim type: `PermissionClaimTypes.Permission` = `"permission"`)
- Permissions đưa vào JWT 1 lần khi login, đọc từ claims khi xử lý request (không query DB)

---

### 2. Login & RefreshToken (Application)

**Files:** `LoginCommandHandler.cs`, `RefreshTokenCommandHandler.cs`

- Inject: `RoleManager<AppRole>`, `IPermissionRepository`
- Lấy role names → resolve role IDs qua `RoleManager.FindByNameAsync`
- Gọi `IPermissionRepository.GetPermissionsByRoleIdsAsync(roleIds)`
- Truyền `permissions` vào `IAccessTokenService.GenerateAccessToken(...)`

---

### 3. Authorization (Infrastructure)

**File:** `DemoCICD.Infrastructure/DependencyInjection/Extensions/ServiceCollectionExtensions.cs`

- `AddPermissionAuthorization()`:
  - Default policy: `RequireAuthenticatedUser()`
  - Policies: `PRODUCT.VIEW`, `PRODUCT.CREATE`, `PRODUCT.UPDATE`, `PRODUCT.DELETE`
  - Đăng ký `PermissionAuthorizationHandler`

**File:** `DemoCICD.Infrastructure/Authorization/PermissionAuthorizationHandler.cs`

- Kiểm tra `context.User.Identity.IsAuthenticated` → nếu không → `context.Fail()`
- Lấy claims `permission` từ user: `context.User.FindAll(PermissionClaimTypes.Permission)`
- So khớp với `requirement.Permission` → `Succeed` hoặc `Fail`

**File:** `DemoCICD.Infrastructure/Authorization/PermissionRequirement.cs`

- `IAuthorizationRequirement` chứa `string Permission`

**File:** `DemoCICD.Infrastructure/Authorization/RequirePermissionAttribute.cs`

- `[Authorize(Policy = permission)]` wrapper
- Dùng: `[RequirePermission(ProductPermissions.View)]` v.v.

**File:** `DemoCICD.Infrastructure/Authorization/ProductPermissions.cs`

- Constants: `View`, `Create`, `Update`, `Delete` (format: `PRODUCT.VIEW`, `PRODUCT.CREATE`, ...)

**File:** `DemoCICD.Infrastructure/Authorization/PermissionClaimTypes.cs`

- Claim type: `"permission"`

---

### 4. Product API (Presentation)

**ProductApi (Minimal API):** `DemoCICD.Presentation/APIs/Products/ProductApi.cs`

- Group: `.RequireAuthorization()`
- Endpoint:
  - `GetProducts`, `GetProductsById` → `RequireAuthorization(ProductPermissions.View)`
  - `CreateProducts` → `RequireAuthorization(ProductPermissions.Create)`
  - `UpdateProducts` → `RequireAuthorization(ProductPermissions.Update)`
  - `DeleteProducts` → `RequireAuthorization(ProductPermissions.Delete)`

**ProductCarterApi:** `DemoCICD.Presentation/APIs/Products/ProductCarterApi.cs`

- Cấu hình tương tự cho V1 và V2

**ProductsController (MVC):** `DemoCICD.Presentation/Controllers/V1/ProductsController.cs`

- Class: `[Authorize]`
- Mỗi action: `[RequirePermission(ProductPermissions.View/Create/Update/Delete)]`

---

### 5. Program.cs

```csharp
// Services
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissionAuthorization();

// Middleware (thứ tự quan trọng)
app.UseAuthentication();
app.UseAuthorization();
```

---

### 6. Persistence

**File:** `DemoCICD.Persistence/DependencyInjection/Extensions/ServiceCollectionExtensions.cs`

- `services.AddTransient<IPermissionRepository, PermissionRepository>();`

**File:** `DemoCICD.Persistence/Repositories/PermissionRepository.cs`

- `GetPermissionsByRoleIdsAsync` → trả về `List<string>` dạng `"FunctionId.ActionId"` (vd: `PRODUCT.VIEW`)

---

## Luồng xử lý

| Tình huống | Kết quả |
|------------|---------|
| Không gửi token / Token không hợp lệ | **401 Unauthorized** |
| Token hợp lệ nhưng thiếu permission | **403 Forbidden** |
| Token hợp lệ + đủ permission | **200 OK** |

---

## Cách test

### 1. Login

```
POST /api/v1/auth/login
Content-Type: application/json

{
  "userName": "superadmin",
  "password": "Password123!"
}
```

### 2. Gọi Product API với token

```
Authorization: Bearer <accessToken>
```

### 3. User test (từ IdentityDataSeeder + RBAC seed)

| User        | Password     | Product Permissions               |
|-------------|--------------|-----------------------------------|
| superadmin  | Password123! | VIEW, CREATE, UPDATE, DELETE (full) |
| manager1    | Password123! | VIEW, CREATE, UPDATE, DELETE (full) |
| staff1      | Password123! | VIEW, CREATE only                 |

---

## File tham chiếu

| File | Mô tả |
|------|-------|
| `IAccessTokenService.cs` | Interface generate JWT với roles + permissions |
| `JwtTokenService.cs` | Implement, thêm permission claims |
| `PermissionAuthorizationHandler.cs` | Custom handler kiểm tra permission từ claims |
| `PermissionRequirement.cs` | Requirement cho policy |
| `RequirePermissionAttribute.cs` | Attribute `[RequirePermission(...)]` |
| `ProductPermissions.cs` | Constants PRODUCT.VIEW, CREATE, UPDATE, DELETE |
| `PermissionClaimTypes.cs` | Claim type "permission" |
| `ServiceCollectionExtensions.cs` (Infrastructure) | AddJwtAuthentication, AddPermissionAuthorization |
| `ServiceCollectionExtensions.cs` (Persistence) | IPermissionRepository registration |
| `LoginCommandHandler.cs` | Login, lấy permissions, gọi GenerateAccessToken |
| `RefreshTokenCommandHandler.cs` | Refresh, lấy permissions, gọi GenerateAccessToken |
| `ProductApi.cs` | Minimal API Product với RequireAuthorization |
| `ProductCarterApi.cs` | Carter Product API với RequireAuthorization |
| `ProductsController.cs` | MVC Product controller với [Authorize], [RequirePermission] |
| `Program.cs` | AddJwtAuthentication, AddPermissionAuthorization, UseAuthentication, UseAuthorization |
