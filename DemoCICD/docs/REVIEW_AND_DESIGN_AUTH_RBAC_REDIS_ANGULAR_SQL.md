# Review & Đề xuất: Auth, RBAC, Redis, Angular User CRUD, SQL Server

Tài liệu tổng hợp: review luồng Login/Auth, RBAC, đề xuất Redis cache, thiết kế màn hình Angular CRUD User + phân quyền, và tối ưu SQL Server.

---

## 1. Review Login + Authentication flow

### 1.1 Luồng hiện tại (tóm tắt)

| Bước | Backend | Ghi chú |
|------|---------|--------|
| 1 | `POST /api/v1/auth/login` (AuthApi.cs) | Carter minimal API |
| 2 | `LoginCommandHandler`: `FindByNameAsync` + `CheckPasswordAsync` | Identity, hash mặc định |
| 3 | `GetRolesAsync` → với mỗi role: `FindByNameAsync` để lấy RoleId | **N+1**: 1 query user + N query role |
| 4 | `GetPermissionsByRoleIdsAsync(roleIds)` | 1 query Permissions, AsNoTracking |
| 5 | `JwtTokenService.GenerateAccessToken(userId, userName, roles, permissions)` | JWT có claims: Sub, Name, Jti, Role, permission |
| 6 | `GenerateRefreshToken` (64 bytes RNG, Base64), lưu DB | RefreshToken entity, 7 ngày |
| 7 | Return `TokenResponse(accessToken, refreshToken, expiresAt)` | |

**Refresh token:** `RefreshTokenCommandHandler` — lấy token từ DB, kiểm tra active, revoke token cũ, load user/roles/permissions, tạo cặp token mới, lưu refresh token mới.

**Logout:** `LogoutCommandHandler` — tìm refresh token, set `RevokedAt`.

**Angular:** Access token chỉ lưu memory; refresh token trong `localStorage`; interceptor gắn Bearer, 401 → refresh (single in-flight) → retry; proactive refresh 60s trước hết hạn; guard gọi `restoreSession()` nếu chưa có access token.

### 1.2 Điểm tốt

- JWT + refresh token, không session server-side, dễ scale.
- Permission đưa vào JWT → authorization không đụng DB mỗi request.
- Angular: access token trong memory (giảm rủi ro XSS), refresh trong localStorage, có comment rõ ràng.
- Interceptor: một lần refresh cho nhiều 401, tránh race; có `X-Skip-Auth-Retry` tránh retry vô hạn.
- Proactive refresh trước khi token hết hạn.
- FluentValidation cho Login/RefreshToken command.
- `ClockSkew = TimeSpan.Zero` cho JWT.

### 1.3 Vấn đề và đề xuất

| Vấn đề | Mức độ | Đề xuất |
|--------|--------|--------|
| **Login: N+1 khi lấy RoleId** | Cao | Dùng một query: join User → UserRoles → Role, hoặc `RoleManager.Roles` join với bảng mapping, lấy list role Id/Name trong một lần. |
| **Rate limiting login** | Trung bình | Thêm rate limit (theo IP hoặc username) cho `/auth/login` để chống brute-force (AspNetCoreRateLimit hoặc Redis). |
| **Refresh token: không revoke hết khi đổi mật khẩu** | Trung bình | Khi đổi mật khẩu (sau này): revoke tất cả refresh token của user (theo UserId). |
| **Logout: chỉ revoke 1 token** | OK | Logout chỉ revoke token hiện tại là đúng; “logout all devices” = endpoint riêng revoke by UserId. |
| **Thông tin user sau login** | Tùy chọn | Có thể trả thêm `userName`, `roles`, `permissions` trong response login (hoặc endpoint `/me`) để Angular không cần decode JWT. |
| **Lockout** | Tùy chọn | Identity đã có lockout; đảm bảo `IdentityOptions.Lockout` được cấu hình và xử lý trong handler (trả lỗi rõ ràng khi bị khóa). |

**Tối ưu LoginCommandHandler (tránh N+1):**

```csharp
// Thay vì: GetRolesAsync → foreach FindByNameAsync
// Dùng: lấy role IDs từ UserRoles (đã có khi load user nếu dùng Include), hoặc:
var userWithRoles = await _context.Users
    .AsNoTracking()
    .Where(u => u.Id == user.Id)
    .Select(u => new { u.Id, RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList() })
    .FirstOrDefaultAsync(ct);
var roleIds = userWithRoles?.RoleIds ?? new List<Guid>();
```

Hoặc giữ `GetRolesAsync` (trả về tên role) và thêm một query lấy tất cả Role theo tên: `RoleManager.Roles.Where(r => roleNames.Contains(r.Name)).Select(r => r.Id).ToListAsync()` — 2 query thay vì 1 + N.

---

## 2. Review Authorization (RBAC) — đã tối ưu chưa?

### 2.1 Kiến trúc hiện tại

- **Permission model:** Role → Permission (RoleId, FunctionId, ActionId); format claim = `FunctionId.ActionId` (vd: `PRODUCT.VIEW`).
- **Khi nào load permission:** Chỉ khi login và refresh token; không query DB mỗi request.
- **Authorization:** `PermissionAuthorizationHandler` đọc claim `permission` từ JWT; so sánh với `PermissionRequirement.Permission`; 401 nếu chưa đăng nhập, 403 nếu thiếu quyền.
- **Đăng ký policy:** Chỉ 4 policy cố định: `ProductPermissions.View/Create/Update/Delete`. Các module ORDER, USER, REPORT, CONFIGURATION chưa có policy tương ứng.

### 2.2 Điểm tốt

- Permission trong JWT → **đã tối ưu**: không đụng DB khi authorize.
- Handler đồng bộ, đơn giản, dễ test.
- Cấu trúc Function/Action/ActionInFunction/Permission linh hoạt cho nhiều module.

### 2.3 Chưa tối ưu / thiếu

| Vấn đề | Đề xuất |
|--------|--------|
| **Chỉ có policy Product** | Thêm policy cho USER, ORDER, REPORT, CONFIGURATION (hoặc đăng ký policy động từ bảng Functions/Actions khi startup). |
| **Policy đăng ký tĩnh** | Có thể dùng **dynamic policy**: một policy tên `Permission` với requirement nhận permission từ attribute; đăng ký một lần `options.AddPolicy("Permission", ...)` với `PermissionRequirement` động (permission string từ `[RequirePermission("USER.VIEW")]`). Hiện tại đã dùng `RequirePermissionAttribute` với policy name = permission string — cần đảm bảo mọi permission dùng trên API đều đã AddPolicy. Cách gọn: **IAuthorizationPolicyProvider** tùy chỉnh: với policy name dạng `PERMISSION:USER.VIEW`, provider trả về policy có requirement `PermissionRequirement("USER.VIEW")`. Như vậy không cần AddPolicy cho từng permission. |
| **Role chỉ trong JWT, không dùng** | Nếu sau này cần “[Authorize(Roles = "Admin")]” có thể giữ; hiện tại permission-based là đủ. |
| **Cập nhật quyền user** | Khi admin đổi role/permission của user, user đang login chỉ nhận quyền mới sau khi refresh token (hoặc đăng nhập lại). Chấp nhận được; nếu cần “cập nhật ngay” có thể rút ngắn TTL access token hoặc có cơ chế invalidate (phức tạp hơn). |

### 2.4 Đề xuất: Dynamic Permission Policy Provider

Để không phải gọi `AddPolicy` cho từng permission (USER.VIEW, USER.CREATE, ORDER.APPROVE, ...):

- Tạo `PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider`. Trong `GetPolicyAsync(string policyName)`:
  - Nếu `policyName.StartsWith("Permission:")` hoặc quy ước tương tự, tách permission string và trả về `new AuthorizationPolicyBuilder().AddRequirements(new PermissionRequirement(permission)).Build()`.
  - Các policy khác delegate về `DefaultAuthorizationPolicyProvider`.
- Đăng ký: `services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>()` (và giữ `PermissionAuthorizationHandler`).
- Attribute giữ: `[Authorize(Policy = "Permission:USER.VIEW")]` hoặc `[RequirePermission("USER.VIEW")]` với policy name = `"Permission:" + permission`.

Như vậy RBAC vẫn không query DB mỗi request và có thể mở rộng permission mới mà không sửa startup.

---

## 3. Đề xuất dùng Redis để cache tối ưu

Hiện tại **dự án chưa dùng Redis**. Đề xuất dưới đây là thiết kế khi bổ sung Redis.

### 3.1 Mục tiêu

- Giảm tải SQL Server.
- Tăng tốc phản hồi cho dữ liệu ít thay đổi hoặc đọc nhiều.

### 3.2 Các kịch bản cache đề xuất

| Đối tượng | Key pattern | TTL | Ghi chú |
|-----------|-------------|-----|--------|
| **Permissions theo RoleIds** | `perms:roles:{roleIdsHash}` hoặc `perms:role:{roleId}` (từng role) | 10–15 phút | Dùng khi login/refresh; hiện đã chỉ gọi lúc issue token, cache giúp giảm DB khi nhiều user cùng role. |
| **User profile (sau khi có /me)** | `user:profile:{userId}` | 5–10 phút | Nếu có API me hoặc get user by id. |
| **Danh sách Roles (dropdown)** | `rbac:roles:list` | 15–30 phút | Cho màn hình phân quyền. |
| **Danh sách Functions/Actions (cây quyền)** | `rbac:functions` | 30 phút | Cho UI chọn quyền theo module. |
| **Refresh token (optional)** | `refresh:{tokenHash}` → userId, expiresAt | Theo expires của token | Có thể dùng Redis làm nguồn sự thật thay vì DB để revoke nhanh; cân nhắc persistence (AOF/RDB). |

### 3.3 Kỹ thuật

- **Interface:** `IDistributedCache` (ASP.NET Core) với implementation Redis (e.g. `Microsoft.Extensions.Caching.StackExchangeRedis`).
- **Serialization:** JSON (System.Text.Json) cho value phức tạp; string cho permission list.
- **Invalidation:**
  - Khi admin sửa Permissions (thêm/xóa role-permission): xóa key `perms:*` (hoặc từng `perms:role:{roleId}` liên quan).
  - Khi sửa Roles/Functions: xóa `rbac:roles:list`, `rbac:functions`.
- **Cache-aside:** Application layer: đọc cache trước; miss thì query DB, ghi cache rồi trả về.

### 3.4 Ví dụ: Cache permissions theo role

```csharp
// IPermissionRepository hoặc wrapper service
public async Task<IReadOnlyList<string>> GetPermissionsByRoleIdsAsync(
    IReadOnlyList<Guid> roleIds,
    CancellationToken cancellationToken = default)
{
    if (roleIds.Count == 0) return Array.Empty<string>();
    var key = "perms:roles:" + string.Join(",", roleIds.OrderBy(x => x));
    var cached = await _cache.GetStringAsync(key, cancellationToken);
    if (cached != null)
        return JsonSerializer.Deserialize<IReadOnlyList<string>>(cached) ?? Array.Empty<string>();
    var permissions = await _permissionRepository.GetPermissionsByRoleIdsAsync(roleIds, cancellationToken);
    await _cache.SetStringAsync(key, JsonSerializer.Serialize(permissions),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) },
        cancellationToken);
    return permissions;
}
```

Khi cập nhật Permissions (trong UseCase hoặc Admin API): gọi `_cache.RemoveAsync("perms:roles:" + ...)` cho các role bị ảnh hưởng hoặc dùng pattern (nếu dùng Redis trực tiếp: `Keys("perms:*")` rồi Delete — cẩn thận production).

### 3.5 Lưu ý

- Connection string Redis trong configuration; dùng cùng instance cho cache và (nếu sau này) rate limit/session.
- Không cache mật khẩu hay access/refresh token value dạng plain.
- Nếu dùng Redis cho refresh token store: cần strategy failover (DB fallback) nếu Redis down.

---

## 4. Thiết kế màn hình Angular: CRUD User + phân quyền

Backend hiện **chưa có API User management**. Thiết kế dưới đây giả định sẽ bổ sung API và Angular dùng API đó.

### 4.1 API Backend cần có (đề xuất)

| Method | Endpoint | Mô tả | Authorization |
|--------|----------|--------|----------------|
| GET | `/api/v1/users` | Danh sách user (paging, filter, search) | USER.VIEW |
| GET | `/api/v1/users/{id}` | Chi tiết user + roles | USER.VIEW |
| POST | `/api/v1/users` | Tạo user (username, email, password, roleIds, ...) | USER.CREATE |
| PUT | `/api/v1/users/{id}` | Cập nhật thông tin + roles | USER.UPDATE |
| DELETE | `/api/v1/users/{id}` | Vô hiệu hóa hoặc xóa (tùy nghiệp vụ) | USER.DELETE |
| GET | `/api/v1/users/roles` | Danh sách role (dropdown) | USER.VIEW hoặc public cho form) |
| GET | `/api/v1/users/functions-actions` | Cây Function + Action (để chọn quyền theo module) | USER.VIEW |

Contract: DTO User (Id, UserName, Email, FullName, Roles[], IsActive, ...), PagedResult, CreateUserCommand, UpdateUserCommand.

### 4.2 Cấu trúc Angular (admin-ui)

- **Route:** `/users` (trong layout đã bảo vệ bởi `authGuard`).
  - `/users` → list.
  - `/users/new` → tạo mới.
  - `/users/:id` → xem/sửa.
  - `/users/:id/permissions` (optional) → màn hình chỉ phân quyền chi tiết (Function/Action).

- **Module / feature:**  
  `views/users/`  
  - `users.routes.ts`  
  - `user-list.component` (bảng, filter, search, sort, pagination, nút Thêm / Sửa / Xóa).  
  - `user-form.component` (create/edit: form User + chọn Role; có thể dùng chung cho create và edit).  
  - `user-detail.component` (optional: xem chi tiết + nút “Phân quyền”).  
  - `user-permission.component` (optional: cây Function/Action checkbox theo role đã chọn hoặc override theo user).

- **Service:**  
  `core/services/user.service.ts` (hoặc `users/user.service.ts`):  
  - `getList(params)`, `getById(id)`, `create(command)`, `update(id, command)`, `delete(id)`, `getRoles()`, `getFunctionsActions()`.

- **Guard (optional):**  
  `userGuard` hoặc directive/hide dựa trên permission: chỉ hiện menu “Users” và route `/users` nếu user có permission `USER.VIEW` (đọc từ JWT decode hoặc API /me).

### 4.3 Sidebar

Trong `_nav.ts` thêm mục (có thể ẩn theo permission):

```ts
{
  name: 'Users',
  url: '/users',
  iconComponent: { name: 'cil-people' },
  attributes: { permission: 'USER.VIEW' }  // nếu có cơ chế hide theo permission
}
```

### 4.4 Wireframe chức năng

- **List:** Bảng: Avatar, UserName, Email, FullName, Roles (badge), Trạng thái, Thao tác (Sửa, Xóa). Thanh tìm kiếm, bộ lọc (role, trạng thái), phân trang.
- **Form (Create/Edit):** Username, Email, Password (chỉ create hoặc “đổi mật khẩu”), FullName, chọn một hoặc nhiều Role (multi select). Nút Lưu / Hủy.
- **Phân quyền (optional):** Hiển thị cây theo Function (PRODUCT, ORDER, USER, ...); mỗi Function có các Action (VIEW, CREATE, UPDATE, DELETE, ...); checkbox theo quyền của role đã chọn (read-only) hoặc override per user nếu backend hỗ trợ.

### 4.5 Permission trên frontend

- Route: `canActivate: [authGuard, permissionGuard('USER.VIEW')]` (nếu có permissionGuard).
- Nút “Tạo mới”: `*ngIf="hasPermission('USER.CREATE')"`.
- Nút “Xóa”: `*ngIf="hasPermission('USER.DELETE')"`.
- Permission lấy từ JWT decode (lưu trong service sau login) hoặc từ API `/me` nếu có.

---

## 5. Đề xuất tối ưu SQL Server

### 5.1 Hiện trạng

- EF Core 7, SQL Server, `DbContext` pool, retry strategy, Lazy loading proxy.
- Bảng: Identity (AppUser, AppRole, UserRoles, ...), RefreshToken, Actions, Functions, ActionInFunctions, Permissions, Products.
- Raw SQL trong `GetProductsQueryHandler` khi sort multi-column: **nối chuỗi SearchTerm vào LIKE** → rủi ro SQL injection.

### 5.2 Tối ưu đề xuất

| Hạng mục | Nội dung |
|----------|----------|
| **1. SQL Injection trong GetProductsQueryHandler** | **Bắt buộc:** Không nối `request.SearchTerm` vào raw SQL. Dùng parameter: `FromSqlRaw("SELECT * FROM Product WHERE Name LIKE {0} OR Description LIKE {1} ...", "%" + request.SearchTerm + "%", "%" + request.SearchTerm + "%")` hoặc chuyển hẳn sang EF (Where + OrderBy động) để tránh injection và dễ bảo trì. |
| **2. Index** | **Permissions:** composite index `(RoleId, FunctionId, ActionId)` hoặc ít nhất `(RoleId)` cho `GetPermissionsByRoleIdsAsync`. **RefreshTokens:** index `(Token)` (unique hoặc không) để lookup nhanh; index `(UserId, RevokedAt)` nếu hay query “tất cả token của user”. **AppUser:** index cho `UserName` (Identity thường đã có). **Products:** index cho `Name`, `Description` nếu filter/search nhiều. |
| **3. Login N+1** | Như mục 1: giảm số query khi lấy role IDs (một query join hoặc 2 query thay vì 1 + N). |
| **4. Count trong GetProductsQueryHandler** | Khi dùng raw SQL branch: `CountAsync()` đang đếm toàn bảng; nên đếm theo đúng bộ lọc (WHERE giống query chính) để phân trang đúng và nhanh hơn khi bảng lớn. |
| **5. Lazy loading** | Tránh N+1 do lazy load; ưu tiên explicit `Include` hoặc projection (`Select`) thay vì truy cập navigation property không cần thiết. |
| **6. Connection string** | Đặt tên rõ (vd `DefaultConnection`) và dùng `ConnectionStrings:DefaultConnection` để tránh nhầm. |
| **7. Typo DbSet** | `AppUses` → đổi thành `Users` (và cập nhật reference nếu có). |

### 5.3 Ví dụ sửa GetProductsQueryHandler (an toàn + đúng count)

- Option A — bỏ raw SQL, dùng EF với sort động:

```csharp
IQueryable<Product> query = _context.Products.AsNoTracking();
if (!string.IsNullOrWhiteSpace(request.SearchTerm))
{
    var term = request.SearchTerm.Trim();
    query = query.Where(p => p.Name.Contains(term) || p.Description.Contains(term));
}
// Sort: dùng reflection hoặc dictionary mapping column name -> Expression
// Rồi OrderBy/OrderByDescending, sau đó Skip/Take hoặc dùng PagedResult.CreateAsync.
var totalCount = await query.CountAsync(cancellationToken);
var items = await query.OrderBy(...).Skip((pageIndex-1)*pageSize).Take(pageSize).ToListAsync(cancellationToken);
```

- Option B — nếu giữ raw SQL: dùng parameter cho search term và sort column whitelist (chỉ cho phép Name, Price, Description), count với cùng điều kiện WHERE (parameterized).

---

## 6. Tóm tắt hành động ưu tiên

| Ưu tiên | Hạng mục | Hành động |
|---------|----------|-----------|
| Cao | Security | Sửa GetProductsQueryHandler: không nối SearchTerm/sort vào raw SQL; dùng parameter hoặc EF. |
| Cao | Auth | Login: bỏ N+1 khi lấy role IDs (1–2 query). |
| Trung bình | RBAC | Thêm policy cho USER/ORDER/REPORT/CONFIGURATION hoặc dùng Dynamic Permission Policy Provider. |
| Trung bình | Redis | Thêm Redis cache: permissions theo role, (optional) roles/functions list; invalidation khi đổi quyền. |
| Trung bình | SQL | Thêm index Permissions(RoleId), RefreshToken(Token); count đúng bộ lọc khi paging. |
| Sau đó | Angular | API User CRUD backend → Angular: routes /users, user-list, user-form, user service, permission guard/menu. |
| Tùy chọn | Auth | Rate limit login; lockout; response login có thêm user/roles/permissions. |

---

*Tài liệu tham chiếu: JWT_AUTH_AND_PERMISSION_SYSTEM.md, RBAC_SEED_DOCUMENTATION.md, AUTH_DESIGN.md, AUTH.md.*
