# Hướng Dẫn Sử Dụng Entity Framework Core Migrations

## Mục Lục
1. [Cài Đặt EF Core Tools](#cài-đặt-ef-core-tools)
2. [Tạo Migration Mới](#tạo-migration-mới)
3. [Cập Nhật Database](#cập-nhật-database)
4. [Xem Danh Sách Migration](#xem-danh-sách-migration)
5. [Rollback Migration](#rollback-migration)
6. [Xóa Migration](#xóa-migration)
7. [Troubleshooting](#troubleshooting)

---

## Cài Đặt EF Core Tools

### Kiểm tra phiên bản .NET
```powershell
dotnet --version
```

### Kiểm tra dotnet-ef đã cài đặt chưa
```powershell
dotnet tool list -g
```

### Cài đặt dotnet-ef (tương thích với .NET 7.0)
```powershell
dotnet tool install --global dotnet-ef --version 7.0.13
```

### Gỡ cài đặt dotnet-ef (nếu cần)
```powershell
dotnet tool uninstall dotnet-ef -g
```

**Lưu ý:** Đảm bảo phiên bản `dotnet-ef` tương thích với phiên bản .NET của dự án:
- .NET 7.0 → dotnet-ef 7.x
- .NET 8.0 → dotnet-ef 8.x
- .NET 10.0 → dotnet-ef 10.x

---

## Tạo Migration Mới

### Cú pháp cơ bản
```powershell
dotnet ef migrations add <TênMigration> --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### Ví dụ
```powershell
# Tạo migration với tên "AddUserTable"
dotnet ef migrations add AddUserTable --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API

# Tạo migration với tên "UpdateProductSchema"
dotnet ef migrations add UpdateProductSchema --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### Tạo migration với output directory tùy chỉnh
```powershell
dotnet ef migrations add <TênMigration> --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API --output-dir Migrations\CustomFolder
```

**Lưu ý:** 
- Migration files sẽ được tạo trong thư mục `src\DemoCICD.Persistence\Migrations\`
- Tên migration nên mô tả rõ ràng thay đổi (ví dụ: `AddUserTable`, `UpdateProductPrice`, `RemoveOldColumn`)

---

## Cập Nhật Database

### Áp dụng tất cả migration chưa được áp dụng
```powershell
dotnet ef database update --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### Áp dụng migration đến một migration cụ thể
```powershell
dotnet ef database update <TênMigration> --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### Ví dụ
```powershell
# Rollback về migration "InitialMigration"
dotnet ef database update InitialMigration --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

**Lưu ý:**
- Lệnh này sẽ kết nối đến database được cấu hình trong `appsettings.json` hoặc `appsettings.Development.json`
- Connection string được lấy từ key `ConnectionStrings:ConnectionStrings` trong file config

---

## Xem Danh Sách Migration

### Xem tất cả migration
```powershell
dotnet ef migrations list --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### Xem migration với thông tin chi tiết
```powershell
dotnet ef migrations list --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API --verbose
```

**Kết quả sẽ hiển thị:**
- Migration đã được áp dụng (có dấu ✓)
- Migration chưa được áp dụng (không có dấu ✓)

---

## Rollback Migration

### Rollback về migration trước đó
```powershell
dotnet ef database update <TênMigrationTrước> --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### Ví dụ
```powershell
# Nếu hiện tại đang ở "AddProduct", rollback về "InitialMigration"
dotnet ef database update InitialMigration --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

**Lưu ý:** 
- Rollback sẽ xóa dữ liệu trong các bảng bị xóa
- Nên backup database trước khi rollback

---

## Xóa Migration

### Xóa migration cuối cùng (chưa được áp dụng)
```powershell
dotnet ef migrations remove --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### Xóa migration đã được áp dụng
1. Rollback database về migration trước đó
2. Sau đó xóa migration file bằng lệnh `remove`

**Lưu ý:**
- Chỉ có thể xóa migration cuối cùng trong danh sách
- Không thể xóa migration đã được áp dụng trực tiếp, phải rollback trước

---

## Troubleshooting

### Lỗi: "Could not load file or assembly 'System.Runtime, Version=10.0.0.0'"

**Nguyên nhân:** Phiên bản `dotnet-ef` không tương thích với .NET version của dự án.

**Giải pháp:**
1. Gỡ cài đặt dotnet-ef hiện tại:
   ```powershell
   dotnet tool uninstall dotnet-ef -g
   ```

2. Cài đặt phiên bản tương thích:
   ```powershell
   # Cho .NET 7.0
   dotnet tool install --global dotnet-ef --version 7.0.13
   
   # Cho .NET 8.0
   dotnet tool install --global dotnet-ef --version 8.0.0
   ```

### Lỗi: "Unable to create an object of type 'ApplicationDbContext'"

**Nguyên nhân:** Không tìm thấy connection string hoặc DbContext chưa được cấu hình đúng.

**Giải pháp:**
1. Kiểm tra connection string trong `appsettings.json` hoặc `appsettings.Development.json`
2. Đảm bảo key là `ConnectionStrings:ConnectionStrings`
3. Kiểm tra DbContext đã được đăng ký trong `Program.cs`

### Lỗi: "The migration 'XXX' has already been applied to the database"

**Nguyên nhân:** Migration đã được áp dụng trước đó.

**Giải pháp:**
- Nếu muốn áp dụng lại, xóa migration trong bảng `__EFMigrationsHistory` trong database
- Hoặc tạo migration mới thay vì sử dụng migration cũ

### Lỗi: "No migrations found"

**Nguyên nhân:** Chưa có migration nào được tạo.

**Giải pháp:**
1. Tạo migration đầu tiên:
   ```powershell
   dotnet ef migrations add InitialMigration --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
   ```

2. Sau đó cập nhật database:
   ```powershell
   dotnet ef database update --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
   ```

---

## Cấu Trúc Thư Mục Migration

```
src/
└── DemoCICD.Persistence/
    └── Migrations/
        ├── ApplicationDbContextModelSnapshot.cs
        ├── 20231028001619_InitialMigration.cs
        ├── 20231028001619_InitialMigration.Designer.cs
        ├── 20231028003653_AddProduct.cs
        └── 20231028003653_AddProduct.Designer.cs
```

---

## Workflow Thông Thường

### 1. Thay đổi Entity hoặc DbContext
```csharp
// Thêm property mới vào Product entity
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } // Thêm mới
}
```

### 2. Tạo migration
```powershell
dotnet ef migrations add AddCreatedAtToProduct --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### 3. Kiểm tra migration file
Mở file migration vừa tạo để xem SQL sẽ được thực thi.

### 4. Cập nhật database
```powershell
dotnet ef database update --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API
```

### 5. Kiểm tra kết quả
- Kiểm tra database đã được cập nhật
- Kiểm tra dữ liệu không bị mất
- Test ứng dụng hoạt động bình thường

---

## Lệnh Tóm Tắt

| Mục đích | Lệnh |
|----------|------|
| Cài đặt EF Core Tools | `dotnet tool install --global dotnet-ef --version 7.0.13` |
| Tạo migration | `dotnet ef migrations add <Tên> --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API` |
| Cập nhật database | `dotnet ef database update --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API` |
| Xem danh sách migration | `dotnet ef migrations list --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API` |
| Xóa migration | `dotnet ef migrations remove --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API` |
| Rollback migration | `dotnet ef database update <TênMigration> --project src\DemoCICD.Persistence --startup-project src\DemoCICD.API` |

---

## Tham Khảo

- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
- [Migration Best Practices](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing)

