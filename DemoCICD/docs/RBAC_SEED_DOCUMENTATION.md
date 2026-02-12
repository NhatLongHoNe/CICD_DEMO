# RBAC Seed Data - Documentation UAT

## 1. Sơ đồ dữ liệu quyền

```
                    ┌─────────────┐
                    │   Action    │
                    │ VIEW, CREATE│
                    │ UPDATE,     │
                    │ DELETE,     │
                    │ APPROVE,    │
                    │ EXPORT      │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐      ┌─────────────┐
                    │ActionInFunc │◄────►│  Function   │
                    │(many-to-many)│      │ Product     │
                    └──────┬──────┘      │ Order       │
                           │             │ User        │
                           │             │ Report      │
                           │             │ Config      │
                           │             └──────┬──────┘
                           │                    │
                    ┌──────▼────────────────────▼──────┐
                    │          Permission              │
                    │  (RoleId, FunctionId, ActionId)  │
                    └──────┬───────────────────────────┘
                           │
                    ┌──────▼──────┐      ┌─────────────┐
                    │  AppRole    │◄────►│  AppUser    │
                    │ SuperAdmin  │      │ (UserRoles) │
                    │ Manager     │      └─────────────┘
                    │ Staff       │
                    └─────────────┘
```

## 2. Danh sách Role & Quyền (bảng)

### 2.1. Actions (6)

| Id     | Name   | SortOrder |
|--------|--------|-----------|
| VIEW   | View   | 1         |
| CREATE | Create | 2         |
| UPDATE | Update | 3         |
| DELETE | Delete | 4         |
| APPROVE| Approve| 5         |
| EXPORT | Export | 6         |

### 2.2. Functions (5 modules)

| Id          | Name        | Url       | Mô tả                |
|-------------|-------------|-----------|----------------------|
| PRODUCT     | Product     | /products | Quản lý sản phẩm     |
| ORDER       | Order       | /orders   | Quản lý đơn hàng     |
| USER        | User        | /users    | Quản lý người dùng   |
| REPORT      | Report      | /reports  | Báo cáo              |
| CONFIGURATION | Configuration | /config | Cấu hình hệ thống    |

### 2.3. ActionInFunction (action áp dụng cho function nào)

| Function      | Actions                                           |
|---------------|---------------------------------------------------|
| PRODUCT       | VIEW, CREATE, UPDATE, DELETE, EXPORT              |
| ORDER         | VIEW, CREATE, UPDATE, DELETE, **APPROVE**, EXPORT |
| USER          | VIEW, CREATE, UPDATE, DELETE                      |
| REPORT        | VIEW, EXPORT                                     |
| CONFIGURATION | VIEW, UPDATE                                     |

### 2.4. Role → Permission (phân quyền không đối xứng)

| Role       | Product     | Order      | User  | Report  | Config  |
|------------|-------------|------------|-------|---------|---------|
| **SuperAdmin** | Full (V,C,U,D,E) | Full (V,C,U,D,A,E) | Full (V,C,U,D) | V,E | V,U |
| **Manager**    | Full (V,C,U,D,E) | Full (V,C,U,D,A,E) | **Không có**   | V,E | V,U |
| **Staff**      | **V,C only**    | **V,C only**      | **Không có**   | **V only** | **Không có** |

- V=View, C=Create, U=Update, D=Delete, A=Approve, E=Export
- Manager: thiếu toàn bộ User.* (test edge case)
- Staff: thiếu Update, Delete, Approve, Export (test phân quyền hạn chế)

### 2.5. Users & Roles

| UserName   | Full Name      | Roles        | Mục đích test                          |
|------------|----------------|--------------|----------------------------------------|
| superadmin | Super Admin    | SuperAdmin   | Full quyền                             |
| manager1   | Nguyen Van Manager | Manager  | 1 role, CRUD+Approve, không User       |
| manager2   | Tran Thi Manager   | Manager  | Tương tự manager1                      |
| staff1     | Le Van Staff       | Staff    | View+Create only                       |
| staff2     | Pham Thi Staff     | Staff    | Tương tự staff1                        |
| staff3     | Hoang Van Staff    | Staff    | Tương tự staff1                        |
| multirole  | Multi Role         | Manager, Staff | User nhiều role, test union quyền   |
| limited    | Limited User       | Staff    | Staff với quyền tối thiểu              |
| noapprove  | No Approve Manager | Manager  | Manager (override test: có thể revoke Approve) |

**Password mặc định tất cả:** `Password123!`

## 3. Code seed

### 3.1. Migration: `20250209100000_SeedRbacData.cs`

- **Actions**: 6 bản ghi
- **Functions**: 5 bản ghi
- **ActionInFunctions**: 20 bản ghi (mapping action-function)
- **AppRoles**: 3 bản ghi
- **Permissions**: ~35 bản ghi (phân quyền không đối xứng)

### 3.2. IdentityDataSeeder (IHostedService)

- Chạy khi app start
- Dùng `UserManager.CreateAsync` → hash password đúng Identity format
- Tạo 9 users + gán UserRoles
- Chỉ chạy nếu chưa tồn tại `superadmin`

### 3.3. Thứ tự thực thi

1. `dotnet ef database update` → Migration seed Actions, Functions, ActionInFunction, Roles, Permissions
2. App start → IdentityDataSeeder tạo Users + UserRoles

## 4. Vì sao dữ liệu giống production?

| Tiêu chí          | Cách triển khai                                      |
|-------------------|------------------------------------------------------|
| Phân quyền không đối xứng | Manager thiếu User.*; Staff chỉ View+Create        |
| Edge case         | User `noapprove` (Manager), `limited` (Staff tối thiểu) |
| Nhiều role        | User `multirole` có Manager + Staff                  |
| Module thực tế    | Product, Order, User, Report, Config                 |
| Action chuẩn      | VIEW, CREATE, UPDATE, DELETE, APPROVE, EXPORT        |
| ID cố định        | Guid hard-code trong `IdentitySeedData.cs`           |
| Không plain-text  | UserManager hash password                            |

## 5. Gợi ý test-case phân quyền

### 5.1. API authorization

```csharp
// Test: Staff gọi PUT /products/{id} → 403
// Test: Manager gọi GET /users → 403 (Manager không có User.VIEW)
// Test: SuperAdmin gọi mọi endpoint → 200
```

### 5.2. UI behavior

- Ẩn nút "Approve" khi Staff
- Ẩn menu "User" khi Manager
- Hiển thị "Export" chỉ khi có quyền EXPORT

### 5.3. Edge case

- User `multirole`: quyền = union(Manager, Staff)
- User `limited`: chỉ Staff, test UI với quyền tối thiểu
- User `noapprove`: Manager nhưng có thể bị revoke APPROVE (nếu có cơ chế override)

## 6. Chạy seed

```powershell
# 1. Áp dụng migration (bao gồm seed RBAC)
dotnet ef database update --project src/DemoCICD.Persistence --startup-project src/DemoCICD.API

# 2. Chạy API (IdentityDataSeeder tự chạy)
dotnet run --project src/DemoCICD.API
```

## 7. Fixed IDs (cho automation test)

```csharp
// Roles
RoleSuperAdminId = 11111111-1111-1111-1111-111111111111
RoleManagerId    = 22222222-2222-2222-2222-222222222222
RoleStaffId      = 33333333-3333-3333-3333-333333333333

// Users
UserSuperAdminId = a1111111-1111-1111-1111-111111111111
UserManager1Id   = b2222222-2222-2222-2222-222222222221
...
```
