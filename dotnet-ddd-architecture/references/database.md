# 資料庫操作指引

## Database First vs Code First 選擇

| 情境 | 建議策略 | 原因 |
|------|---------|------|
| 資料表已存在 | Database First | 避免重複定義，直接從資料庫產生 Entity |
| 全新專案 | Code First | 由程式碼控制結構，方便版本管理 |
| 資料表頻繁變更 | Database First | DBA 控管資料庫，開發人員同步 |
| 需要版本控制 | Code First | Migration 記錄所有變更歷史 |
| 多人協作 | Code First | 避免資料表結構衝突 |

## Database First 流程

### 1. 初次 Scaffold（產生 Entity 與 DbContext）

```powershell
# 在 Infrastructure 專案目錄執行
dotnet ef dbcontext scaffold "Server=localhost;Database=YourDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Entities -c YourDbContext --context-dir Data --force
```

參數說明：
- `-o Entities`: Entity 輸出目錄
- `-c YourDbContext`: DbContext 類別名稱
- `--context-dir Data`: DbContext 輸出目錄
- `--force`: 覆蓋既有檔案

### 2. 資料表變更後重新 Scaffold

```powershell
# 重新產生所有 Entity
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Entities -c YourDbContext --context-dir Data --force

# 只產生特定資料表
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Entities -c YourDbContext --context-dir Data --force -t Products -t Categories
```

### 3. Scaffold 後的處理

Scaffold 產生的 Entity 通常需要調整：

**原始產生的 Entity：**
```csharp
public partial class Product
{
    public string ProductId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal? Price { get; set; }
    // OnConfiguring 與連線字串寫在 DbContext 中...
}
```

**建議調整：**
1. 移除 DbContext 中的 `OnConfiguring`，改用 DI 注入
2. 將 Entity 屬性加上 XML 註解
3. 將 DbContext 設定移到 Program.cs

**調整後的 DbContext：**
```csharp
public partial class YourDbContext : DbContext
{
    public YourDbContext(DbContextOptions<YourDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Scaffold 自動產生的設定保留
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
```

### 4. 注意事項

- **手動修改會被覆蓋**: 重新 Scaffold 會覆蓋手動修改的內容
- **使用 partial class**: 若需擴充 Entity，使用 partial class 在另一個檔案
- **備份設定**: 重新 Scaffold 前備份 OnModelCreating 的自訂設定

## Code First 流程

### 1. 定義 Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YourProject.Domain.Entities
{
    [Table("Products")]
    public class Product
    {
        [Key]
        [StringLength(50)]
        public string ProductID { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Status { get; set; } = 1;

        public DateTime CreateDate { get; set; } = DateTime.Now;

        // 導航屬性
        public string? CategoryID { get; set; }
        
        [ForeignKey("CategoryID")]
        public virtual Category? Category { get; set; }
    }
}
```

### 2. 設定 DbContext

```csharp
using Microsoft.EntityFrameworkCore;

namespace YourProject.Infrastructure.Data
{
    public class YourDbContext : DbContext
    {
        public YourDbContext(DbContextOptions<YourDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fluent API 設定
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductID);
                
                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");

                // 關聯設定
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryID)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
```

### 3. 建立 Migration

```powershell
# 在 API 或 Infrastructure 專案目錄執行

# 新增 Migration
dotnet ef migrations add InitialCreate --project ../YourProject.Infrastructure --startup-project ../YourProject.API

# 新增後續變更
dotnet ef migrations add AddProductCategory --project ../YourProject.Infrastructure --startup-project ../YourProject.API
```

### 4. 更新資料庫

```powershell
# 更新到最新 Migration
dotnet ef database update --project ../YourProject.Infrastructure --startup-project ../YourProject.API

# 更新到特定 Migration
dotnet ef database update AddProductCategory --project ../YourProject.Infrastructure --startup-project ../YourProject.API

# 回滾到前一個 Migration
dotnet ef database update PreviousMigrationName --project ../YourProject.Infrastructure --startup-project ../YourProject.API

# 回滾到初始狀態（刪除所有變更）
dotnet ef database update 0 --project ../YourProject.Infrastructure --startup-project ../YourProject.API
```

### 5. 移除 Migration

```powershell
# 移除最後一個 Migration（尚未套用到資料庫）
dotnet ef migrations remove --project ../YourProject.Infrastructure --startup-project ../YourProject.API

# 若已套用到資料庫，需先回滾
dotnet ef database update PreviousMigration --project ../YourProject.Infrastructure --startup-project ../YourProject.API
dotnet ef migrations remove --project ../YourProject.Infrastructure --startup-project ../YourProject.API
```

### 6. 查看 Migration 狀態

```powershell
# 列出所有 Migration
dotnet ef migrations list --project ../YourProject.Infrastructure --startup-project ../YourProject.API

# 產生 SQL 腳本（不實際執行）
dotnet ef migrations script --project ../YourProject.Infrastructure --startup-project ../YourProject.API -o migration.sql

# 產生從特定 Migration 到最新的 SQL
dotnet ef migrations script PreviousMigration --project ../YourProject.Infrastructure --startup-project ../YourProject.API -o migration.sql
```

## 資料庫連線設定

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=YourDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;",
    "ReadOnlyConnection": "Server=replica;Database=YourDB;User Id=readonly;Password=ReadPassword;TrustServerCertificate=True;"
  }
}
```

### Program.cs 註冊

```csharp
builder.Services.AddDbContext<YourDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // 開發環境顯示詳細錯誤
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

## 初始資料 (Seed Data)

### Code First 方式

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // 預設資料
    modelBuilder.Entity<Category>().HasData(
        new Category { CategoryID = "CAT001", CategoryName = "電子產品", Status = 1, CreateDate = DateTime.Now },
        new Category { CategoryID = "CAT002", CategoryName = "書籍", Status = 1, CreateDate = DateTime.Now }
    );
}
```

### 執行時植入

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
    
    // 確保資料庫已建立
    context.Database.EnsureCreated();
    
    // 或使用 Migration
    // context.Database.Migrate();
    
    // 植入初始資料
    if (!context.Categories.Any())
    {
        context.Categories.AddRange(
            new Category { CategoryID = "CAT001", CategoryName = "電子產品", Status = 1 },
            new Category { CategoryID = "CAT002", CategoryName = "書籍", Status = 1 }
        );
        context.SaveChanges();
    }
}
```

## 常見問題與解決方案

### 問題 1: Scaffold 失敗

```
Error: Unable to find provider assembly
```

**解決**: 確認已安裝必要套件
```powershell
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### 問題 2: Migration 失敗

```
Error: Unable to create an object of type 'YourDbContext'
```

**解決**: 確認 Program.cs 有正確註冊 DbContext，或建立 Design Time Factory

```csharp
public class YourDbContextFactory : IDesignTimeDbContextFactory<YourDbContext>
{
    public YourDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<YourDbContext>();
        optionsBuilder.UseSqlServer("ConnectionString");
        return new YourDbContext(optionsBuilder.Options);
    }
}
```

### 問題 3: 連線字串錯誤

**解決**: 檢查 appsettings.json 與 User Secrets

```powershell
# 使用 User Secrets 存放敏感資訊
dotnet user-secrets init --project YourProject.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;" --project YourProject.API
```

## 最佳實踐

1. **連線字串管理**: 開發環境用 User Secrets，正式環境用環境變數或 Azure Key Vault
2. **Migration 命名**: 使用清楚的名稱，例如 `AddProductTable`、`UpdateUserEmail`
3. **測試環境**: 測試用獨立資料庫，避免影響開發或正式環境
4. **交易控制**: 複雜操作使用 UnitOfWork 確保資料一致性
5. **效能優化**: 查詢使用 `AsNoTracking()`、避免 N+1 查詢、建立適當索引
6. **版本控制**: Code First 的 Migration 檔案納入版控，方便追蹤變更歷史
