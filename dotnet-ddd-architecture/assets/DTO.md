# DTO 樣板

放置路徑：Domain/DTOs/{Feature}/

## {Feature}DTO（查詢回傳）

```csharp
namespace {Project}.Domain.DTOs.{Feature}
{
    /// <summary>
    /// {Feature} 查詢回傳 DTO
    /// </summary>
    public class {Feature}DTO
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

## Edit{Feature}DTO（新增/編輯）

```csharp
using System.ComponentModel.DataAnnotations;

namespace {Project}.Domain.DTOs.{Feature}
{
    /// <summary>
    /// {Feature} 新增/編輯 DTO
    /// </summary>
    public class Edit{Feature}DTO
    {
        /// <summary>
        /// 主鍵（編輯時必填，新增時留空）
        /// </summary>
        public string? {Entity}ID { get; set; }

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

## Search{Feature}DTO（搜尋條件）

```csharp
using {Project}.Domain.DTOs.Share;

namespace {Project}.Domain.DTOs.{Feature}
{
    /// <summary>
    /// {Feature} 搜尋條件 DTO
    /// </summary>
    public class Search{Feature}DTO : SearchDTO
    {
        /// <summary>
        /// 關鍵字（搜尋名稱）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 狀態篩選（null: 全部, 0: 停用, 1: 啟用）
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// 開始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 結束日期
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
}
```

## 共用基底 DTO

放置路徑：Domain/DTOs/Share/

### SearchDTO.cs

```csharp
namespace {Project}.Domain.DTOs.Share
{
    /// <summary>
    /// 搜尋條件基底 DTO
    /// </summary>
    public class SearchDTO
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
        /// 排序欄位
        /// </summary>
        public string? OrderBy { get; set; }

        /// <summary>
        /// 排序方向（asc/desc）
        /// </summary>
        public string OrderDirection { get; set; } = "desc";
    }
}
```

### PaginationDTO.cs

```csharp
namespace {Project}.Domain.DTOs.Share
{
    /// <summary>
    /// 分頁回傳 DTO
    /// </summary>
    public class PaginationDTO<T>
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
        /// 資料清單
        /// </summary>
        public List<T> DataList { get; set; } = new List<T>();

        /// <summary>
        /// 總頁數
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCounts / PerPage);
    }
}
```

## 使用注意事項

1. **DTO 分層**: Service 層使用 Domain 的 DTO，API 層使用自己的 Request/Response
2. **驗證規則**: 在 DTO 上加 DataAnnotations 進行基本驗證
3. **複雜驗證**: 複雜業務規則驗證放在 Service 層
4. **命名規則**: DTO 名稱清楚表達用途（{Feature}DTO、Edit{Feature}DTO、Search{Feature}DTO）
5. **屬性說明**: 使用 XML 註解說明每個屬性用途
