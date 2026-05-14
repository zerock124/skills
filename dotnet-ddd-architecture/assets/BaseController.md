# BaseController 與 Response 樣板

## BaseController

放置路徑：API/Controllers/BaseController.cs

```csharp
using {Project}.API.Models.Shared;
using Microsoft.AspNetCore.Mvc;

namespace {Project}.API.Controllers
{
    /// <summary>
    /// 控制器基底類別
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// 成功回應（無資料）
        /// </summary>
        protected IActionResult SuccessResponse(string message = "操作成功")
        {
            var response = new BaseResponse
            {
                Success = true,
                Message = message
            };
            return Ok(response);
        }

        /// <summary>
        /// 成功回應（含資料）
        /// </summary>
        protected IActionResult SuccessResponse<T>(T data, string message = "操作成功")
        {
            var response = new DataResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
            return Ok(response);
        }

        /// <summary>
        /// 失敗回應
        /// </summary>
        protected IActionResult ErrorResponse(string message = "操作失敗", int statusCode = 400)
        {
            var response = new BaseResponse
            {
                Success = false,
                Message = message
            };
            return StatusCode(statusCode, response);
        }

        /// <summary>
        /// 驗證失敗回應
        /// </summary>
        protected IActionResult ValidationErrorResponse(string message = "資料驗證失敗")
        {
            return ErrorResponse(message, 400);
        }

        /// <summary>
        /// 找不到資料回應
        /// </summary>
        protected IActionResult NotFoundResponse(string message = "查無資料")
        {
            return ErrorResponse(message, 404);
        }

        /// <summary>
        /// 未授權回應
        /// </summary>
        protected IActionResult UnauthorizedResponse(string message = "未授權")
        {
            return ErrorResponse(message, 401);
        }

        /// <summary>
        /// 伺服器錯誤回應
        /// </summary>
        protected IActionResult InternalError(Exception ex, BaseResponse? response = null)
        {
            if (response == null)
            {
                response = new BaseResponse
                {
                    Success = false,
                    Message = "伺服器發生錯誤"
                };
            }

            // 開發環境可以回傳詳細錯誤訊息
            #if DEBUG
            response.Message += $": {ex.Message}";
            #endif

            return StatusCode(500, response);
        }
    }
}
```

## Response Models

放置路徑：API/Models/Shared/

### BaseResponse.cs

```csharp
namespace {Project}.API.Models.Shared
{
    /// <summary>
    /// 基礎回應模型
    /// </summary>
    public class BaseResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 時間戳記
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

### DataResponse.cs

```csharp
namespace {Project}.API.Models.Shared
{
    /// <summary>
    /// 資料回應模型
    /// </summary>
    /// <typeparam name="T">資料類型</typeparam>
    public class DataResponse<T> : BaseResponse
    {
        /// <summary>
        /// 回傳資料
        /// </summary>
        public T? Data { get; set; }
    }
}
```

### PaginationResponse.cs

```csharp
namespace {Project}.API.Models.Shared
{
    /// <summary>
    /// 分頁回應模型
    /// </summary>
    /// <typeparam name="T">資料類型</typeparam>
    public class PaginationResponse<T> : BaseResponse
    {
        /// <summary>
        /// 當前頁數
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 每頁筆數
        /// </summary>
        public int PerPage { get; set; }

        /// <summary>
        /// 總筆數
        /// </summary>
        public int TotalCounts { get; set; }

        /// <summary>
        /// 總頁數
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 資料清單
        /// </summary>
        public List<T> DataList { get; set; } = new List<T>();
    }
}
```

## Request Models

放置路徑：API/Models/{Feature}/

### Create{Feature}Request.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace {Project}.API.Models.{Feature}
{
    /// <summary>
    /// 新增 {Feature} 請求模型
    /// </summary>
    public class Create{Feature}Request
    {
        /// <summary>
        /// 名稱
        /// </summary>
        [Required(ErrorMessage = "名稱為必填")]
        [StringLength(100, ErrorMessage = "名稱長度不可超過 100 字元")]
        public string {Name} { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(500, ErrorMessage = "描述長度不可超過 500 字元")]
        public string? Description { get; set; }

        /// <summary>
        /// 狀態（0: 停用, 1: 啟用）
        /// </summary>
        [Range(0, 1, ErrorMessage = "狀態值必須為 0 或 1")]
        public int Status { get; set; } = 1;
    }
}
```

### Update{Feature}Request.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace {Project}.API.Models.{Feature}
{
    /// <summary>
    /// 更新 {Feature} 請求模型
    /// </summary>
    public class Update{Feature}Request
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        [Required(ErrorMessage = "ID 為必填")]
        public string {Entity}ID { get; set; } = string.Empty;

        /// <summary>
        /// 名稱
        /// </summary>
        [Required(ErrorMessage = "名稱為必填")]
        [StringLength(100, ErrorMessage = "名稱長度不可超過 100 字元")]
        public string {Name} { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(500, ErrorMessage = "描述長度不可超過 500 字元")]
        public string? Description { get; set; }

        /// <summary>
        /// 狀態（0: 停用, 1: 啟用）
        /// </summary>
        [Range(0, 1, ErrorMessage = "狀態值必須為 0 或 1")]
        public int Status { get; set; }
    }
}
```

### Search{Feature}Request.cs

```csharp
namespace {Project}.API.Models.{Feature}
{
    /// <summary>
    /// 搜尋 {Feature} 請求模型
    /// </summary>
    public class Search{Feature}Request
    {
        /// <summary>
        /// 當前頁數（從 1 開始）
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// 每頁筆數
        /// </summary>
        public int PerPage { get; set; } = 10;

        /// <summary>
        /// 關鍵字
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 狀態篩選
        /// </summary>
        public int? Status { get; set; }
    }
}
```

### {Feature}Response.cs

```csharp
namespace {Project}.API.Models.{Feature}
{
    /// <summary>
    /// {Feature} 回應模型
    /// </summary>
    public class {Feature}Response
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        public string {Entity}ID { get; set; } = string.Empty;

        /// <summary>
        /// 名稱
        /// </summary>
        public string {Name} { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 狀態（0: 停用, 1: 啟用）
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 狀態文字
        /// </summary>
        public string StatusText => Status == 1 ? "啟用" : "停用";

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// 建立者
        /// </summary>
        public string CreateUser { get; set; } = string.Empty;
    }
}
```

## 使用範例

```csharp
[HttpPost(Name = "Create{Feature}")]
public async Task<IActionResult> Create([FromBody] Create{Feature}Request request)
{
    try
    {
        // 驗證 ModelState
        if (!ModelState.IsValid)
        {
            return ValidationErrorResponse("資料驗證失敗");
        }

        var dto = _mapper.Map<Edit{Feature}DTO>(request);
        var result = await _service.Create(dto);

        if (!result)
        {
            return ErrorResponse("新增失敗");
        }

        return SuccessResponse("新增成功");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "新增{Feature}失敗");
        return InternalError(ex);
    }
}
```

## 注意事項

1. **統一格式**: 所有 API 回應都應使用標準 Response 格式
2. **驗證層級**: Request Model 做基本驗證，Service 做業務驗證
3. **異常處理**: Controller 統一處理異常，避免洩漏敏感資訊
4. **狀態碼**: 正確使用 HTTP 狀態碼（200, 400, 401, 404, 500）
5. **文件化**: 使用 XML 註解與 Swagger，確保 API 文件完整
