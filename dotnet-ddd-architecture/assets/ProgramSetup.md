# Program.cs 啟動設定樣板

放置路徑：API/Program.cs

```csharp
using {Project}.Infrastructure.Data;
using {Project}.Infrastructure.Repository;
using {Project}.Infrastructure.UnitOfWork;
using {Project}.Domain.Interface;
using {Project}.Domain.UnitOfWork;
using {Project}.Service.Interface;
using {Project}.Service.Implements;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ========== 日誌設定 ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ========== 服務註冊 ==========

// Controller 與 JSON 設定
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // JSON 序列化設定
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持原始大小寫
        options.JsonSerializerOptions.WriteIndented = true; // 格式化輸出
        options.JsonSerializerOptions.DefaultIgnoreCondition = 
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull; // 忽略 null 值
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "{Project} API",
        Version = "v1",
        Description = "{Project} RESTful API 文件",
        Contact = new OpenApiContact
        {
            Name = "開發團隊",
            Email = "dev@example.com"
        }
    });

    // 加入 XML 註解
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // JWT 認證設定
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. 請輸入 'Bearer' [space] 然後輸入 token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// 資料庫連線
builder.Services.AddDbContext<{Project}DbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// JWT 認證
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey 未設定");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // 移除預設 5 分鐘寬限期
    };
});

builder.Services.AddAuthorization();

// ========== 依賴注入註冊 ==========

// UnitOfWork & Repository
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// 特定 Repository（若有）
// builder.Services.AddScoped<I{Feature}Repository, {Feature}Repository>();

// Service 層
// builder.Services.AddScoped<I{Feature}Service, {Feature}Service>();

// HttpClient（若需要呼叫外部 API）
builder.Services.AddHttpClient();

// Memory Cache（若需要）
builder.Services.AddMemoryCache();

// ========== 中介軟體管線 ==========

var app = builder.Build();

// 開發環境設定
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "{Project} API v1");
        options.RoutePrefix = string.Empty; // 首頁直接顯示 Swagger UI
    });
}

// HTTPS 重定向
app.UseHttpsRedirection();

// CORS（必須在 Authentication 之前）
app.UseCors("AllowSpecificOrigins");

// 靜態檔案（若有）
// app.UseStaticFiles();

// 路由
app.UseRouting();

// 認證 & 授權（順序重要）
app.UseAuthentication();
app.UseAuthorization();

// Controller 端點
app.MapControllers();

// 健康檢查端點
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.Now }))
   .WithTags("Health");

// 根路徑資訊
app.MapGet("/", () => Results.Ok(new 
{ 
    name = "{Project} API", 
    version = "v1", 
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.Now 
}))
.WithTags("Info");

try
{
    Log.Information("啟動 {Project} API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "{Project} API 啟動失敗");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
```

## appsettings.json 範例

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database={Project}DB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyAtLeast32CharactersLong!",
    "Issuer": "{Project}API",
    "Audience": "{Project}Client",
    "ExpirationMinutes": 60
  },
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173",
    "https://yourdomain.com"
  ],
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database={Project}DB_Dev;User Id=sa;Password=DevPassword;TrustServerCertificate=True;"
  },
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173"
  ],
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

## 必要的 NuGet 套件清單

```xml
<!-- API 專案 -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.*" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.*" />

<!-- Infrastructure 專案 -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.*" />

<!-- Test 專案 -->
<PackageReference Include="xunit" Version="2.6.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.*" />
<PackageReference Include="Moq" Version="4.20.*" />
<PackageReference Include="coverlet.collector" Version="6.0.*" />
```

## 注意事項

1. **順序很重要**: CORS 必須在 UseAuthentication 之前，UseAuthentication 必須在 UseAuthorization 之前
2. **環境設定**: 開發與正式環境分離設定檔，敏感資訊不要提交到版控
3. **XML 註解**: 需在 csproj 中啟用 `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
4. **DI 註冊**: 依層級分類註冊，方便維護與查找
5. **日誌**: 使用 Serilog 統一日誌格式，方便追蹤與分析
