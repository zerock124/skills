# 錯誤處理與驗證指引

## 驗證層級

### 1. Request Model 驗證（API 層）

使用 DataAnnotations 進行基本格式驗證。

```csharp
public class CreateProductRequest
{
    [Required(ErrorMessage = "商品名稱為必填")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "商品名稱長度需介於 2-100 字元")]
    public string ProductName { get; set; } = string.Empty;

    [Range(0, 999999, ErrorMessage = "價格必須介於 0-999999")]
    public decimal Price { get; set; }

    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    public string? ContactEmail { get; set; }

    [RegularExpression(@"^09\d{8}$", ErrorMessage = "手機號碼格式不正確")]
    public string? Phone { get; set; }
}
```

Controller 中檢查 ModelState：

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
{
    if (!ModelState.IsValid)
    {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage);
        return ValidationErrorResponse(string.Join(", ", errors));
    }

    // 繼續處理...
}
```

### 2. 業務規則驗證（Service 層）

複雜的業務邏輯驗證放在 Service。

```csharp
public async Task<(bool Success, string Message)> Create(EditProductDTO data)
{
    // 業務規則驗證
    if (_repository.Exists(p => p.ProductName == data.ProductName))
    {
        return (false, "商品名稱已存在");
    }

    if (data.Price < 0)
    {
        return (false, "價格不可為負數");
    }

    // 檢查關聯資料
    var category = _categoryRepository.Get(c => c.CategoryID == data.CategoryID);
    if (category == null)
    {
        return (false, "分類不存在");
    }

    if (category.Status == 0)
    {
        return (false, "此分類已停用，無法新增商品");
    }

    // 驗證通過，執行新增
    var entity = _mapper.Map<Product>(data);
    entity.ProductID = Guid.NewGuid().ToString();
    entity.CreateDate = DateTime.Now;
    
    _repository.Create(entity);
    await _unitOfWork.SaveChangesAsync();
    
    return (true, "新增成功");
}
```

Controller 使用：

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
{
    if (!ModelState.IsValid)
    {
        return ValidationErrorResponse();
    }

    var dto = _mapper.Map<EditProductDTO>(request);
    var (success, message) = await _service.Create(dto);

    if (!success)
    {
        return ErrorResponse(message);
    }

    return SuccessResponse(message);
}
```

### 3. 資料庫約束驗證

Entity Framework 會自動檢查資料庫約束，需妥善處理 `DbUpdateException`。

```csharp
public async Task<bool> Create(EditOrderDTO data)
{
    try
    {
        var entity = _mapper.Map<Order>(data);
        _repository.Create(entity);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "資料儲存失敗: {Message}", ex.Message);
        
        // 根據例外類型提供明確訊息
        if (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            throw new Exception("資料重複，無法新增");
        }
        if (ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
        {
            throw new Exception("關聯資料不存在");
        }
        
        throw new Exception("資料儲存失敗，請稍後再試");
    }
}
```

## 異常處理策略

### 1. Controller 層統一異常處理

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
{
    try
    {
        if (!ModelState.IsValid)
        {
            return ValidationErrorResponse();
        }

        var dto = _mapper.Map<EditProductDTO>(request);
        var result = await _service.Create(dto);

        if (!result)
        {
            return ErrorResponse("新增失敗");
        }

        return SuccessResponse("新增成功");
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "未授權存取");
        return UnauthorizedResponse();
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "參數錯誤");
        return ErrorResponse(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "系統錯誤");
        return InternalError(ex);
    }
}
```

### 2. 全域異常處理中介軟體（選用）

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全域異常處理");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new BaseResponse
        {
            Success = false,
            Message = "系統發生錯誤"
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "未授權";
                break;
            case ArgumentException:
            case InvalidOperationException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = exception.Message;
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

// 在 Program.cs 註冊
app.UseMiddleware<GlobalExceptionMiddleware>();
```

## 自訂例外類別

```csharp
namespace {Project}.Domain.Exceptions
{
    /// <summary>
    /// 業務規則驗證失敗例外
    /// </summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }

    /// <summary>
    /// 資料不存在例外
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string entity, string id) 
            : base($"{entity} (ID: {id}) 不存在") { }
    }

    /// <summary>
    /// 資料重複例外
    /// </summary>
    public class DuplicateException : Exception
    {
        public DuplicateException(string field, string value) 
            : base($"{field} '{value}' 已存在") { }
    }
}
```

使用範例：

```csharp
public async Task<ProductDTO?> Get(string id)
{
    var entity = _repository.Get(p => p.ProductID == id);
    if (entity == null)
    {
        throw new NotFoundException("Product", id);
    }
    return _mapper.Map<ProductDTO>(entity);
}

public async Task<bool> Create(EditProductDTO data)
{
    if (_repository.Exists(p => p.ProductName == data.ProductName))
    {
        throw new DuplicateException("ProductName", data.ProductName);
    }

    if (data.Price < 0)
    {
        throw new BusinessException("價格不可為負數");
    }

    // 執行新增...
}
```

## 驗證最佳實踐

1. **分層驗證**: API 驗證格式，Service 驗證業務規則
2. **明確訊息**: 錯誤訊息要清楚具體，方便前端顯示與使用者理解
3. **記錄日誌**: 錯誤要記錄完整 stack trace，但不回傳給前端
4. **統一格式**: 所有錯誤回應使用標準 Response 格式
5. **避免洩密**: 不要將敏感資訊（如資料庫連線字串、內部路徑）洩漏到錯誤訊息
6. **HTTP 狀態碼**: 正確使用狀態碼（200, 400, 401, 404, 500）
7. **非空檢查**: 關聯資料存取前必須檢查是否為 null
8. **交易回滾**: 複雜操作失敗時確保資料一致性

## 常見驗證情境

| 情境 | 驗證層級 | 處理方式 |
|------|---------|---------|
| 必填欄位 | Request Model | `[Required]` |
| 字串長度 | Request Model | `[StringLength]` |
| 數值範圍 | Request Model | `[Range]` |
| Email 格式 | Request Model | `[EmailAddress]` |
| 正規表示式 | Request Model | `[RegularExpression]` |
| 資料重複 | Service | 查詢後判斷 |
| 關聯存在 | Service | 查詢關聯資料 |
| 狀態限制 | Service | 業務邏輯判斷 |
| 權限檢查 | Service / Filter | 依使用者角色判斷 |
| 資料庫約束 | Infrastructure | Catch DbUpdateException |
