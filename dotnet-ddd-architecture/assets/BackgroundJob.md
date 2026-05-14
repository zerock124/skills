# 背景排程 (Background Job) 樣板

## 使用 IHostedService

放置路徑：API/Jobs/{Feature}Job.cs

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using {Project}.Service.Interface;

namespace {Project}.API.Jobs
{
    /// <summary>
    /// {Feature} 背景工作
    /// </summary>
    public class {Feature}Job : BackgroundService
    {
        private readonly ILogger<{Feature}Job> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _period;

        public {Feature}Job(
            ILogger<{Feature}Job> logger,
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            
            // 從設定讀取執行週期（分鐘）
            var intervalMinutes = configuration.GetValue<int>("Jobs:{Feature}Job:IntervalMinutes", 60);
            _period = TimeSpan.FromMinutes(intervalMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Feature}Job 已啟動", nameof({Feature}Job));

            // 使用 PeriodicTimer（.NET 6+）
            using var timer = new PeriodicTimer(_period);

            try
            {
                // 立即執行一次（可選）
                await DoWorkAsync(stoppingToken);

                // 定期執行
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await DoWorkAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("{Feature}Job 正在停止", nameof({Feature}Job));
            }
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("{Feature}Job 開始執行", nameof({Feature}Job));

                // 建立 Scope 以取得 Scoped 服務
                using var scope = _serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<I{Feature}Service>();

                // 執行實際工作
                var result = await service.DoBackgroundWork();

                _logger.LogInformation("{Feature}Job 執行完成，處理 {Count} 筆資料", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Feature}Job 執行失敗", nameof({Feature}Job));
                // 不重新拋出，讓 Job 繼續執行
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Feature}Job 正在停止", nameof({Feature}Job));
            await base.StopAsync(cancellationToken);
        }
    }
}
```

## 使用 Quartz.NET（更彈性的排程）

**安裝套件:**
```powershell
dotnet add package Quartz
dotnet add package Quartz.Extensions.Hosting
```

**Job 實作:**
```csharp
using Quartz;
using {Project}.Service.Interface;

namespace {Project}.API.Jobs
{
    [DisallowConcurrentExecution] // 防止重複執行
    public class {Feature}QuartzJob : IJob
    {
        private readonly ILogger<{Feature}QuartzJob> _logger;
        private readonly I{Feature}Service _service;

        public {Feature}QuartzJob(
            ILogger<{Feature}QuartzJob> logger,
            I{Feature}Service service)
        {
            _logger = logger;
            _service = service;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Quartz Job {JobName} 開始執行", context.JobDetail.Key);

                // 從 JobDataMap 取得參數
                var data = context.JobDetail.JobDataMap;
                var parameter = data.GetString("Parameter");

                // 執行工作
                var result = await _service.DoBackgroundWork();

                _logger.LogInformation("Quartz Job {JobName} 完成，處理 {Count} 筆", context.JobDetail.Key, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quartz Job {JobName} 失敗", context.JobDetail.Key);
                throw; // Quartz 會記錄失敗次數
            }
        }
    }
}
```

**Program.cs 註冊:**
```csharp
// IHostedService 方式
builder.Services.AddHostedService<{Feature}Job>();

// Quartz.NET 方式
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("{Feature}Job");
    
    q.AddJob<{Feature}QuartzJob>(opts => opts.WithIdentity(jobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("{Feature}Job-trigger")
        .WithCronSchedule("0 0 2 * * ?") // 每天凌晨 2 點執行
        // 或使用簡單週期
        // .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever())
    );
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
```

**appsettings.json 設定:**
```json
{
  "Jobs": {
    "{Feature}Job": {
      "IntervalMinutes": 60,
      "Enabled": true
    }
  },
  "Quartz": {
    "quartz.scheduler.instanceName": "{Project}Scheduler",
    "quartz.threadPool.threadCount": 3
  }
}
```

## Cron 表示式參考

| 表示式 | 說明 |
|--------|------|
| `0 0 2 * * ?` | 每天凌晨 2:00 |
| `0 */30 * * * ?` | 每 30 分鐘 |
| `0 0 9-17 * * MON-FRI` | 週一到週五 9:00-17:00 每小時 |
| `0 0 12 1 * ?` | 每月 1 號中午 12:00 |
| `0 0 0 * * SUN` | 每週日午夜 12:00 |

## 注意事項

1. **Scope 管理**: BackgroundService 是 Singleton，需手動建立 Scope 才能使用 Scoped 服務
2. **異常處理**: Job 內部要處理例外，避免 Job 停止運作
3. **併發控制**: 使用 `[DisallowConcurrentExecution]` 防止同時執行多個實例
4. **日誌記錄**: 記錄開始、結束、處理筆數、錯誤
5. **可控制性**: 提供設定檔控制執行週期與啟用/停用
