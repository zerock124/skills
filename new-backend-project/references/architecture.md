# 後端分層架構

## 依賴形狀：扁平星狀，不是傳統洋蔥

```
                  ┌──────────────────────┐
                  │  <Name>.API / .Web   │  ← DI 接線層，唯一知道所有實作的地方
                  └──────────┬───────────┘
             ┌───────────────┼───────────────┐
             ▼               ▼               ▼
    ┌────────────────┐  ┌─────────┐  ┌──────────────┐
    │ .Infrastructure│  │ .Service│  │              │
    └────────┬───────┘  └────┬────┘  │              │
             └───────┬───────┘       │              │
                     ▼               ▼              │
                  ┌──────────────────────────────────┘
                  │        <Name>.Domain             │  ← 最底層，不參考任何專案
                  └──────────────────────────────────┘
```

| 專案 | 參考 | 職責 |
|---|---|---|
| `.Domain` | 無 | Entities、DTOs、Enums、Configs、**所有介面** |
| `.Infrastructure` | Domain | EF Core `DbContext`、`GenericRepository`、`UnitOfWork`、外部服務實作 |
| `.Service` | Domain | 業務邏輯。**不參考 Infrastructure** |
| `.API` / `.Web` | Domain + Infrastructure + Service | Controller、Filter、Middleware、DI 註冊 |
| `.Tests` | Service + Domain | xunit 單元測試 |

**Service 不參考 Infrastructure 是刻意的**。Service 只認得 Domain 的 `IGenericRepository<T>` / `IUnitOfWork` 介面，實作在最外層才綁定。這讓 Service 可以在測試中直接餵假 repository，不必啟動 EF Core。

若某個 Service 需要 Infrastructure 的能力（寄信、打外部 API、存檔），作法是：**介面定義在 Domain/Interface，實作放 Infrastructure，最外層註冊**——而不是加專案參考。

## 資料夾配置

```
src/<Name>.Domain/
  Entities/            Category.cs                    ← 單數命名
  DTOs/
    Share/             BaseResponse.cs, SearchDTO.cs, PaginationDTO.cs
    <Module>/          CategoryDTO.cs, EditCategoryDTO.cs, SearchCategoryDTO.cs
  Enums/
  Configs/             AppSettingsConfig.cs, JwtSettingConfig.cs
  Interface/           IGenericRepository.cs, IUnitOfWork.cs
  Utilities/

src/<Name>.Infrastructure/
  Data/                EntityDbContext.cs, Migrations/
  Implement/           GenericRepository.cs, UnitOfWork.cs
  Helpers/

src/<Name>.Service/
  Interface/           ICategoryService.cs
  Implement/           CategoryService.cs
  Mappings/            CategoryProfile.cs
  Helpers/             ServiceExceptionHelper.cs

src/<Name>.API/
  Controllers/         BaseController.cs, CategoryController.cs
  Models/
    Share/             PaginationResponse.cs
    <Module>/          CategoryModels.cs                ← 一模組一檔，內含所有 Request/Response
  Mappings/            CategoryMapping.cs
  Extensions/          ConfigurationExtensions.cs, ServiceCollectionExtensions.cs,
                       MiddlewareExtensions.cs, LoggingExtensions.cs
  Filters/             ApiExceptionFilter.cs, ClaimsFilter.cs
  Middlewares/         ApiExceptionHandlingMiddleware.cs
  Helpers/             ApiExceptionResponseHelper.cs
```

## 命名規則

| 類型 | 慣例 | 範例 |
|---|---|---|
| Entity | 單數 PascalCase | `Category` |
| DTO | `<動作><Module>DTO` | `CategoryDTO`、`EditCategoryDTO`、`SearchCategoryDTO` |
| Service 介面／實作 | `I<Module>Service` / `<Module>Service` | `ICategoryService` / `CategoryService` |
| API Model | `<Module><動作>Request` / `Response` | `CategoryEditRequest`、`CategoryResponse` |
| Controller | `<Module>Controller` | `CategoryController` |

## 新增一個業務模組時要動的檔案

固定這 8 個位置，缺一不可：

1. `Domain/Entities/<Module>.cs`
2. `Domain/DTOs/<Module>/`（DTO、EditDTO、SearchDTO）
3. `Service/Interface/I<Module>Service.cs`
4. `Service/Implement/<Module>Service.cs`
5. `Service/Mappings/`（Entity ↔ DTO）
6. `API/Models/<Module>/<Module>Models.cs`
7. `API/Controllers/<Module>Controller.cs` + `API/Mappings/`（DTO ↔ Request/Response）
8. `Extensions/ServiceCollectionExtensions.cs` 加一行 `AddTransient`

## 型別對應鏈

`Request` → `DTO` → `Entity` → `DTO` → `Response`，兩次 AutoMapper 轉換：

- API 層的 Mapping 負責 `Request ↔ DTO`、`DTO ↔ Response`
- Service 層的 Mapping 負責 `DTO ↔ Entity`

Controller 不碰 Entity，Service 不碰 Request/Response。這道界線是本架構最容易被打破的地方，請守住。
