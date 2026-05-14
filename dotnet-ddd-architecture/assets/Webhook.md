# Webhook 處理樣板

## Webhook Controller

放置路徑：API/Controllers/WebhookController.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using {Project}.API.Filters;
using {Project}.Service.Interface;
using System.Text;
using System.Security.Cryptography;

namespace {Project}.API.Controllers
{
    /// <summary>
    /// Webhook 接收端點
    /// </summary>
    [ApiController]
    [Route("api/webhook")]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly IWebhookService _webhookService;
        private readonly IConfiguration _configuration;

        public WebhookController(
            ILogger<WebhookController> logger,
            IWebhookService webhookService,
            IConfiguration configuration)
        {
            _logger = logger;
            _webhookService = webhookService;
            _configuration = configuration;
        }

        /// <summary>
        /// Line Webhook 端點
        /// </summary>
        [HttpPost("line")]
        [ServiceFilter(typeof(LineWebhookValidationFilter))]
        public async Task<IActionResult> LineWebhook([FromBody] LineWebhookRequest request)
        {
            try
            {
                _logger.LogInformation("收到 Line Webhook，事件數: {Count}", request.Events?.Length ?? 0);

                if (request.Events == null || request.Events.Length == 0)
                {
                    return Ok();
                }

                foreach (var evt in request.Events)
                {
                    await _webhookService.ProcessLineEvent(evt);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Line Webhook 處理失敗");
                // Webhook 通常回傳 200，避免發送方重試
                return Ok();
            }
        }

        /// <summary>
        /// 通用 Webhook 端點（需簽章驗證）
        /// </summary>
        [HttpPost("generic")]
        public async Task<IActionResult> GenericWebhook([FromBody] dynamic payload)
        {
            try
            {
                // 驗證簽章
                var signature = Request.Headers["X-Signature"].FirstOrDefault();
                if (!VerifySignature(payload.ToString(), signature))
                {
                    _logger.LogWarning("Webhook 簽章驗證失敗");
                    return Unauthorized();
                }

                _logger.LogInformation("收到 Webhook: {Payload}", payload);

                await _webhookService.ProcessGenericWebhook(payload);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook 處理失敗");
                return Ok(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 驗證 Webhook 簽章
        /// </summary>
        private bool VerifySignature(string payload, string? signature)
        {
            if (string.IsNullOrEmpty(signature))
                return false;

            var secret = _configuration["Webhook:Secret"] ?? throw new InvalidOperationException("Webhook Secret 未設定");
            var computedSignature = ComputeHmacSha256(payload, secret);

            return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha256(string payload, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
```

## Webhook 驗證 Filter

放置路徑：API/Filters/LineWebhookValidationFilter.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace {Project}.API.Filters
{
    /// <summary>
    /// Line Webhook 簽章驗證 Filter
    /// </summary>
    public class LineWebhookValidationFilter : IAsyncActionFilter
    {
        private readonly ILogger<LineWebhookValidationFilter> _logger;
        private readonly IConfiguration _configuration;

        public LineWebhookValidationFilter(
            ILogger<LineWebhookValidationFilter> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 讀取請求內容
            context.HttpContext.Request.EnableBuffering();
            using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.HttpContext.Request.Body.Position = 0;

            // 取得簽章
            var signature = context.HttpContext.Request.Headers["X-Line-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Line Webhook 缺少簽章");
                context.Result = new UnauthorizedResult();
                return;
            }

            // 驗證簽章
            var channelSecret = _configuration["Line:ChannelSecret"] ?? throw new InvalidOperationException("Line Channel Secret 未設定");
            var computedSignature = ComputeHmacSha256(body, channelSecret);

            if (!signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Line Webhook 簽章驗證失敗");
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }

        private static string ComputeHmacSha256(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
```

## Webhook Service

放置路徑：Service/Implements/WebhookService.cs

```csharp
using {Project}.Service.Interface;
using {Project}.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace {Project}.Service.Implements
{
    public class WebhookService : IWebhookService
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        // 其他必要的 Repository 與 Service

        public WebhookService(
            ILogger<WebhookService> logger,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task ProcessLineEvent(LineEvent lineEvent)
        {
            _logger.LogInformation("處理 Line 事件: {Type}", lineEvent.Type);

            switch (lineEvent.Type)
            {
                case "message":
                    await HandleMessageEvent(lineEvent);
                    break;
                case "follow":
                    await HandleFollowEvent(lineEvent);
                    break;
                case "unfollow":
                    await HandleUnfollowEvent(lineEvent);
                    break;
                default:
                    _logger.LogWarning("未處理的事件類型: {Type}", lineEvent.Type);
                    break;
            }
        }

        private async Task HandleMessageEvent(LineEvent lineEvent)
        {
            // 處理訊息事件
            _logger.LogInformation("收到訊息: {Text}", lineEvent.Message?.Text);
            
            // 儲存訊息記錄、自動回覆等...
            
            await Task.CompletedTask;
        }

        private async Task HandleFollowEvent(LineEvent lineEvent)
        {
            // 處理加入好友事件
            _logger.LogInformation("使用者加入: {UserId}", lineEvent.Source?.UserId);
            
            // 儲存使用者資訊、發送歡迎訊息等...
            
            await Task.CompletedTask;
        }

        private async Task HandleUnfollowEvent(LineEvent lineEvent)
        {
            // 處理取消好友事件
            _logger.LogInformation("使用者離開: {UserId}", lineEvent.Source?.UserId);
            
            // 更新使用者狀態等...
            
            await Task.CompletedTask;
        }

        public async Task ProcessGenericWebhook(dynamic payload)
        {
            // 處理通用 Webhook
            _logger.LogInformation("處理通用 Webhook");
            
            // 依 payload 內容執行相應邏輯
            
            await Task.CompletedTask;
        }
    }
}
```

## Program.cs 註冊

```csharp
// 註冊 Filter
builder.Services.AddScoped<LineWebhookValidationFilter>();

// 註冊 Service
builder.Services.AddScoped<IWebhookService, WebhookService>();

// 若需要發送 Webhook
builder.Services.AddHttpClient<IWebhookSender, WebhookSender>();
```

## appsettings.json

```json
{
  "Line": {
    "ChannelSecret": "your-line-channel-secret",
    "ChannelAccessToken": "your-line-channel-access-token"
  },
  "Webhook": {
    "Secret": "your-webhook-secret"
  }
}
```

## 注意事項

1. **簽章驗證**: 務必驗證來源真實性，防止偽造請求
2. **快速回應**: Webhook 應快速回傳 200，避免發送方逾時重試
3. **非同步處理**: 複雜處理應放到背景佇列，避免阻塞 Webhook 端點
4. **重試機制**: 發送方可能重試，需處理重複事件（冪等性）
5. **日誌記錄**: 記錄所有收到的 Webhook，方便追蹤與除錯
6. **錯誤處理**: Webhook 失敗時通常仍回傳 200，避免無限重試
