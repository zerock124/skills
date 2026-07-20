# 後端程式碼慣例

## Program.cs 保持極薄

`Program.cs` 只留編號步驟與註解，實作全部推到 `Extensions/`。目標是掃一眼就知道啟動流程做了哪些事。

```csharp
using <Name>.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. 載入設定與註冊資料庫 (Configuration)
var config = builder.Services.AddAppSettings(builder.Configuration);

// 2. 註冊依賴注入服務 (Dependency Injection)
builder.Services.AddApplicationDefaults(builder.Configuration); // Controllers, Session, AutoMapper
builder.Services.AddSecurity(config);                           // JWT, CORS
builder.Services.AddSwagger();                                  // API 文件
builder.Services.AddBusinessServices();                         // 業務邏輯服務

// 3. 設定全域日誌 (Logging)
builder.Host.UseCustomSerilog(config);

var app = builder.Build();

// 4. 配置 HTTP 請求處理管線 (Middleware Pipeline)
app.UseCustomMiddleware();

// 5. 註冊端點 (Endpoints)
app.MapControllers();

app.Run();
```

要建立的 Extension 檔案（`src/<Name>.API/Extensions/`）：

| 檔案 | 內容 |
|---|---|
| `ConfigurationExtensions.cs` | `AddAppSettings` — 綁定設定物件、註冊 `DbContext` |
| `ServiceCollectionExtensions.cs` | `AddApplicationDefaults` / `AddSecurity` / `AddSwagger` / `AddBusinessServices` |
| `MiddlewareExtensions.cs` | `UseCustomMiddleware` — 管線順序 |
| `LoggingExtensions.cs` | `UseCustomSerilog` — 選配 |

## JSON 與路由

```csharp
services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    // 保留 C# 屬性名稱原樣 (PascalCase)，不自動轉為 camelCase
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
```

**PascalCase 是硬性約定**。前端 orval 產生的型別直接吃後端 Swagger，兩邊都是 PascalCase 才對得上；改成 camelCase 會讓前端錯誤處理（讀 `Message` 欄位）整片失效。

## DI 生命週期

```csharp
public static IServiceCollection AddBusinessServices(this IServiceCollection services)
{
    // 資料存取：Scoped，與 DbContext 同生命週期
    services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    // 業務服務：一律 Transient，手動逐條列出
    services.AddTransient<ICategoryService, CategoryService>();

    return services;
}
```

刻意**不使用** Scrutor 之類的組件掃描自動註冊。手動列舉雖然囉唆，但註冊清單本身就是一份可讀的服務目錄，也避免誤註冊到不該公開的類別。

## 回應格式三件套

| 類別 | 位置 | 用途 |
|---|---|---|
| `BaseResponse` | Domain/DTOs/Share | `{ Success, Message }`，錯誤回應與無資料的成功回應 |
| `DataResponse<T>` | Domain/DTOs/Share | 繼承 `BaseResponse`，加 `T? Data` |
| `PaginationResponse<T>` | API/Models/Share | 繼承 `BaseResponse`，`Data` 內含分頁資訊與 `DataList` |

Controller 回傳一律 `Ok(res)`，不要用 `ActionResult<T>`：

```csharp
[Tags("Category")]
public class CategoryController(ICategoryService service, IMapper mapper) : BaseController
{
    [HttpPost]
    [ProducesResponseType(typeof(DataResponse<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Edit(CategoryEditRequest request)
    {
        var dto = mapper.Map<EditCategoryDTO>(request);
        var result = await service.EditAsync(dto);
        return Ok(new DataResponse<CategoryResponse> { Data = mapper.Map<CategoryResponse>(result) });
    }
}
```

## 例外處理：雙保險 + 集中映射

- `Middlewares/ApiExceptionHandlingMiddleware.cs` 掛在管線最外層，接住 filter 管不到的例外
- `Filters/ApiExceptionFilter.cs` 全域 filter，處理 action 內的例外
- 兩者共用 `Helpers/ApiExceptionResponseHelper.cs` 做狀態碼映射

映射規則：

| 例外 | 狀態碼 | 訊息 |
|---|---|---|
| `ArgumentException` | 400 | 原始訊息 |
| `KeyNotFoundException` | 404 | 原始訊息 |
| `InvalidOperationException` | 409 | 原始訊息 |
| 其他 | 500 | 遮蔽為「系統發生錯誤，請稍後再試」 |

Service 層擲出前三種例外來表達業務錯誤，不要自己回傳錯誤碼。未預期的例外訊息一律遮蔽，避免洩漏內部細節。

## Middleware 順序

```csharp
app.UseMiddleware<ApiExceptionHandlingMiddleware>();  // 最外層
app.UseRequestBuffering();                            // 非 multipart 才 EnableBuffering
app.MigrateDatabase();                                // 選配
app.UseSwagger(); app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowSpecificOrigins");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```

## 設定檔

```jsonc
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*",
  "AppSettings": {
    "IsDev": true,
    "ConnectionString": "",        // 空字串佔位，實際值走 user-secrets / 環境變數
    "TimeZone": "Asia/Taipei"
  },
  "JwtSetting": {
    "Issuer": "<Name>",
    "Audience": "<Name>",
    "SecretKey": "",               // 同上，絕不寫進版控
    "ExpireHours": 8
  }
}
```

綁定到 `Domain/Configs/AppSettingsConfig`，以 **Singleton** 註冊。

**機密值處理**：開發用 `dotnet user-secrets set "AppSettings:ConnectionString" "..."`，部署用環境變數（`AppSettings__ConnectionString`）。不要把明碼密碼寫進 `appsettings.json` —— 這是既有專案的技術債，新專案別複製。

**不要用 `services.BuildServiceProvider()` 取設定**。`AddAppSettings` 回傳已綁定的設定物件，後續 extension 直接以參數接收即可。

## 資料存取

EF Core（無 Dapper）。Service 直接注入泛型 repository，不做 per-entity repository：

```csharp
public class CategoryService(IGenericRepository<Category> repo, IUnitOfWork uow) : ICategoryService
{
    public async Task<CategoryDTO> EditAsync(EditCategoryDTO dto)
    {
        var entity = repo.Get(x => x.Id == dto.Id)
            ?? throw new KeyNotFoundException("查無此分類");
        // ...修改 entity...
        repo.Update(entity);
        await uow.SaveChangesAsync();   // 提交只走 UnitOfWork
        return mapper.Map<CategoryDTO>(entity);
    }
}
```

`IGenericRepository` **沒有** `SaveChangesAsync`，提交一律經 `IUnitOfWork`。這樣同一次請求內跨多個 repository 的變更才會落在同一個交易裡。

Migration 只用 `dotnet ef migrations add <Name>` 產生，**不要手改** migration 檔案。
