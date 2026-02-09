# Phân Tích Kiến Trúc Hệ Thống DemoCICD

## 1. Tổng Quan

**DemoCICD** là ứng dụng ASP.NET Core 7.0 được thiết kế theo **Clean Architecture** (Kiến trúc Sạch), kết hợp **CQRS** (Command Query Responsibility Segregation) và **MediatR** cho việc xử lý request/response. Hệ thống hỗ trợ đa phiên bản API (V1, V2), container hóa bằng Docker, và có bộ test kiến trúc để đảm bảo tuân thủ dependency rules.

---

## 2. Sơ Đồ Kiến Trúc Tổng Quan

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              DEMOCICD.API (Host)                                  │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Swagger    │  │  Carter      │  │  Serilog     │  │ Exception Middleware │   │
│  │  API Ver.   │  │  Minimal API │  │  Logging     │  │                      │   │
│  └─────────────┘  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          PRESENTATION LAYER                                       │
│  • Controllers (MVC) - ProductsController V1, V2                                  │
│  • Minimal APIs - ProductApi (MapProductApiV1, MapProductApiV2)                   │
│  • Carter Modules - ProductCarterApi                                              │
│  • Abstractions: ApiController, ApiEndpoint                                       │
└─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          APPLICATION LAYER (Use Cases)                            │
│  • MediatR Pipeline: Validation → Performance → Transaction                       │
│  • Command Handlers (Create, Update, Delete Product)                              │
│  • Query Handlers (GetProducts, GetProductById)                                   │
│  • Domain Event Handlers (SendEmail, SendSms when Product changed)                │
│  • AutoMapper, FluentValidation                                                   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                         │
                          ┌──────────────┴──────────────┐
                          ▼                             ▼
┌──────────────────────────────────┐  ┌─────────────────────────────────────────────┐
│     DOMAIN LAYER (Core)          │  │     CONTRACT (Shared Abstractions)           │
│  • Entities: Product, Identity   │  │  • ICommand, IQuery, IDomainEvent            │
│  • Repositories interfaces       │  │  • ICommandHandler, IQueryHandler            │
│  • Unit of Work                  │  │  • Result, PagedResult, Error                │
│  • Domain Exceptions             │  │  • DTOs, Validators (V1, V2)                 │
└──────────────────────────────────┘  └─────────────────────────────────────────────┘
                          │
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────────────────────┐
│  PERSISTENCE    │ │ INFRA.DAPPER    │ │  INFRASTRUCTURE (Placeholder)            │
│  • EF Core      │ │ • Dapper        │ │  • Dùng cho các implementation khác      │
│  • SQL Server   │ │ • Raw SQL       │ │    ngoài Persistence                      │
│  • Migrations   │ │ • IProductRepo  │ │                                           │
│  • Identity     │ └─────────────────┘ └─────────────────────────────────────────┘
└─────────────────┘
```

---

## 3. Cấu Trúc Project và Phụ Thuộc

| Project | Mô tả | Phụ thuộc |
|---------|-------|-----------|
| **DemoCICD.API** | Entry point, cấu hình host | Application, Presentation, Persistence, Infrastructure, Infrastructure.Dapper |
| **DemoCICD.Presentation** | Controllers, Minimal APIs, Carter modules | Application, Contract |
| **DemoCICD.Application** | Use cases, CQRS handlers, Behaviors | Domain, Contract, **Persistence** |
| **DemoCICD.Domain** | Entities, interfaces, business logic | Contract |
| **DemoCICD.Contract** | Shared abstractions, DTOs, validators | Không (root) |
| **DemoCICD.Persistence** | EF Core, DbContext, Migrations | Domain, Contract |
| **DemoCICD.Infrastructure** | Placeholder / cross-cutting | Application, Persistence |
| **DemoCICD.Infrastructure.Dapper** | Dapper repositories | Domain |
| **DemoCICD.Architecture.Tests** | Kiểm tra tuân thủ kiến trúc | Tất cả projects |

### Lưu ý về Clean Architecture

- **Application** phụ thuộc **Persistence** do sử dụng `ApplicationDbContext` và `SqlServerRetryingExecutionStrategy` trong `TransactionPipelineBehavior` (SQL-SERVER-STRATEGY-1).
- Theo README: việc dùng RawQuery với EF cho sort multi-columns yêu cầu Application tham chiếu Persistence.
- Architecture Tests bỏ qua kiểm tra `PersistenceNamespace` trong rule Application.

---

## 4. Luồng Xử Lý Request (Request Flow)

### 4.1. CQRS với MediatR

```
HTTP Request
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Presentation (Controller / Minimal API / Carter)                │
│  - Nhận request, gọi sender.Send(Command) hoặc sender.Send(Query)│
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  MediatR Pipeline (thứ tự)                                       │
│  1. ValidationPipelineBehavior  - FluentValidation               │
│  2. PerformancePipelineBehavior - Đo hiệu năng                   │
│  3. TransactionPipelineBehavior - Transaction (chỉ Command)      │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  CommandHandler / QueryHandler                                   │
│  - Thực thi business logic                                       │
│  - Gọi Repository / UnitOfWork                                   │
│  - Publish Domain Events (nếu có)                                │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Persistence (EF Core) hoặc Infrastructure.Dapper (Dapper)       │
│  - Truy cập SQL Server                                           │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2. Domain Events (In-Memory)

- **ICommand / IQuery** → MediatR `IRequest` → CommandBus/QueryBus (`ISender`)
- **IDomainEvent** → MediatR `INotification` → EventBus (`IPublisher`)

Ví dụ: `ProductCreated`, `ProductDeleted` → `SendEmailWhenProductChangedEventHandler`, `SendSmsWhenProductChangedEventHandler`.

---

## 5. Các Layer Chi Tiết

### 5.1. API (DemoCICD.API)

- **Framework**: ASP.NET Core 7.0
- **Chức năng**:
  - Cấu hình Serilog
  - Đăng ký MediatR, AutoMapper, FluentValidation
  - Swagger với FluentValidation rules
  - API Versioning (`Asp.Versioning`)
  - Carter (modular minimal APIs)
  - ExceptionHandlingMiddleware
  - SQL Server + Retry options
  - Dapper Infrastructure

- **Endpoints**:
  - `products-minimal-show-on-swagger`: Minimal API cho Product (V1, V2)
  - Carter: ProductCarterApi
  - Controllers: ProductsController V1, V2

### 5.2. Presentation

- **Controllers**: `ProductsController` (V1, V2)
- **Minimal APIs**: `ProductApi` – MapProductApiV1, MapProductApiV2
- **Carter**: `ProductCarterApi` – module-based routing
- Base: `ApiController`, `ApiEndpoint`

### 5.3. Application

- **Handlers**:
  - V1: Create/Update/Delete Product (Commands), GetProducts/GetProductById (Queries)
  - V2: Tương tự, có thể mở rộng khác
- **Behaviors**:
  - `ValidationPipelineBehavior`: FluentValidation trước khi xử lý
  - `PerformancePipelineBehavior`: Đo thời gian xử lý
  - `TransactionPipelineBehavior`: Transaction với EF (chỉ cho class kết thúc bằng `Command`)
- **AutoMapper**: `ServiceProfile`

### 5.4. Domain

- **Entities**:
  - `Product`: CreateProduct, Update
  - Identity: `AppUser`, `AppRole`, `Action`, `Function`, `Permission`, `ActionInFunction`
- **Abstractions**:
  - `IUnitOfWork` (EF)
  - `IRepositoryBase<T, TKey>`
  - Dapper: `IUnitOfWork`, `IProductRepository`, `IGenericRepository`
- **Exceptions**: `DomainException`, `NotFoundException`, `BadRequestException`, `ProductException`

### 5.5. Contract

- **Message**: `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, `IDomainEvent`, handlers tương ứng
- **Shared**: `Result`, `Result<T>`, `PagedResult`, `Error`, `ValidationResult`, `IValidationResult`
- **Services**: DTOs, Commands, Queries, Response, Validators cho Product V1, V2

### 5.6. Persistence

- **DbContext**: `ApplicationDbContext` (kế thừa `IdentityDbContext<AppUser, AppRole, Guid>`)
- **Entities mapped**: Product, AppUser, Action, Function, ActionInFunction, Permission
- **Configurations**: Fluent API cho từng entity
- **Migrations**: InitialMigration, AddProduct
- **Options**: `SqlServerRetryOptions` – Retry execution strategy
- **Repositories**: `RepositoryBase<T, TKey>`, `EFUnitOfWork`

### 5.7. Infrastructure.Dapper

- **Dependencies**: Dapper, Microsoft.Data.SqlClient
- **Repositories**: `ProductRepository` – raw SQL cho CRUD Product
- **UnitOfWork**: `UnitOfWork` implement `Domain.Abstractions.Dappers.IUnitOfWork`
- **Lưu ý**: Cùng database với EF; `IUnitOfWork` của Dapper khác interface với EF.

---

## 6. Cơ Sở Dữ Liệu

- **Database**: SQL Server
- **ORM**: Entity Framework Core 7.0 (chính), Dapper (bổ sung)
- **Identity**: ASP.NET Core Identity (AppUser, AppRole)
- **Migrations**: Quản lý bằng EF Core Migrations (xem `MIGRATION_GUIDE.md`)

### Strategy Transaction

- **SQL-SERVER-STRATEGY-1** (đang dùng): Dùng `ApplicationDbContext` và `SqlServerRetryingExecutionStrategy` – có retry, nhưng Application phải tham chiếu Persistence.
- **SQL-SERVER-STRATEGY-2**: Dùng `IUnitOfWork` với `TransactionScope` – giữ Clean Architecture, nhưng không dùng retry execution strategy.

---

## 7. DevOps & Deployment

### 7.1. Docker

- **Base image**: `mcr.microsoft.com/dotnet/aspnet:7.0`
- **Build**: multi-stage (build → publish → final)
- **Expose**: Port 80
- **Lưu ý**: Dockerfile chưa copy `DemoCICD.Infrastructure.Dapper` – cần bổ sung nếu dùng Dapper trong container.

### 7.2. CI/CD

- Không có cấu hình `.github/workflows` hoặc Azure DevOps pipeline trong repo hiện tại.
- Tên solution gợi ý dự án dùng cho demo CI/CD.

---

## 8. Code Quality & Testing

- **Architecture Tests**: NetArchTest.Rules – kiểm tra dependency giữa các layer
- **Style**: StyleCop.Analyzers, SonarAnalyzer (qua `Directory.Build.props`)
- **Naming conventions**:
  - Command: kết thúc bằng `Command`
  - CommandHandler: kết thúc bằng `CommandHandler`, sealed
  - Query: kết thúc bằng `Query`
  - QueryHandler: kết thúc bằng `QueryHandler`, sealed

---

## 9. Công Nghệ Sử Dụng

| Công nghệ | Phiên bản / Ghi chú |
|-----------|---------------------|
| .NET | 7.0 |
| ASP.NET Core | 7.0 |
| Entity Framework Core | 7.0.13 |
| MediatR | 12.1.1 |
| FluentValidation | 11.8.0 |
| AutoMapper | 12.0.1 |
| Carter | 7.2.0 |
| Dapper | 2.1.15 |
| Serilog | 7.0.0 |
| Swashbuckle (Swagger) | 6.5.0 |

---

## 10. Sơ Đồ Dependency Giữa Các Project

```
                    ┌──────────────┐
                    │   Contract   │
                    └──────┬───────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌───────────────┐  ┌───────────────┐  ┌─────────────────┐
│    Domain     │  │  Application  │  │   Presentation  │
└───────┬───────┘  └───────┬───────┘  └────────┬────────┘
        │                  │                   │
        │         ┌────────┴────────┐          │
        │         ▼                 ▼          │
        │  ┌──────────────┐  ┌──────────────┐  │
        └─►│  Persistence │  │Infrastructure│◄─┘
           └──────────────┘  └──────┬───────┘
                    │               │
                    └───────┬───────┘
                            │
                    ┌───────▼────────┐
                    │   DemoCICD.API │
                    └────────────────┘
                            │
                    ┌───────▼────────┐
                    │ Infra.Dapper   │
                    └────────────────┘
```

---

## 11. Khuyến Nghị

1. **Dockerfile**: Thêm `DemoCICD.Infrastructure.Dapper` vào các bước COPY nếu API sử dụng Dapper.
2. **IUnitOfWork**: Có hai interface (`Domain.Abstractions.IUnitOfWork` và `Domain.Abstractions.Dappers.IUnitOfWork`) – cần rõ ràng use case cho từng loại để tránh nhầm lẫn khi đăng ký DI.
3. **CI/CD**: Thêm pipeline (GitHub Actions, Azure DevOps) cho build, test, và deploy.
4. **Health checks**: Cân nhắc thêm endpoint health check cho container và load balancer.

---

*Tài liệu được tạo tự động dựa trên phân tích codebase DemoCICD.*
