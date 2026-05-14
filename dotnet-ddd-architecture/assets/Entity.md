# Entity 樣板

放置路徑：Domain/Entities/{Entity}.cs

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace {Project}.Domain.Entities
{
    /// <summary>
    /// {Entity} 實體
    /// </summary>
    [Table("{TableName}")]
    public class {Entity}
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        [Key]
        [Column("{Entity}ID")]
        [StringLength(50)]
        public string {Entity}ID { get; set; } = string.Empty;

        /// <summary>
        /// 名稱
        /// </summary>
        [Column("{Name}")]
        [StringLength(100)]
        public string {Name} { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [Column("Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 狀態（0: 停用, 1: 啟用）
        /// </summary>
        [Column("Status")]
        public int Status { get; set; } = 1;

        /// <summary>
        /// 建立時間
        /// </summary>
        [Column("CreateDate")]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 建立者
        /// </summary>
        [Column("CreateUser")]
        [StringLength(50)]
        public string CreateUser { get; set; } = string.Empty;

        /// <summary>
        /// 更新時間
        /// </summary>
        [Column("UpdateDate")]
        public DateTime? UpdateDate { get; set; }

        /// <summary>
        /// 更新者
        /// </summary>
        [Column("UpdateUser")]
        [StringLength(50)]
        public string? UpdateUser { get; set; }

        // 導航屬性
        // public virtual {RelatedEntity}? {RelatedEntity} { get; set; }
        // public virtual ICollection<{RelatedEntity}>? {RelatedEntities} { get; set; }
    }
}
```

## 使用注意事項

1. **Database First**: 若使用 Database First，此檔案由 scaffold 自動產生，手動修改可能被覆蓋
2. **Code First**: 若使用 Code First，需手動定義後執行 migration
3. **欄位對應**: `[Column]` 屬性定義資料庫欄位名稱，確保與資料表一致
4. **關聯設定**: 導航屬性需配合 DbContext 中的 `HasOne`、`HasMany` 設定
5. **必填欄位**: 使用 `[Required]` 或非 null 型別確保資料完整性
