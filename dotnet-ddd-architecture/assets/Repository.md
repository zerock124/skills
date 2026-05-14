# Repository 樣板

## IGenericRepository 介面

放置路徑：Domain/Interface/IGenericRepository.cs

```csharp
using System.Linq.Expressions;

namespace {Project}.Domain.Interface
{
    /// <summary>
    /// 泛型 Repository 介面
    /// </summary>
    /// <typeparam name="T">實體類型</typeparam>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// 取得所有資料查詢（不執行）
        /// </summary>
        IQueryable<T> GetAll();

        /// <summary>
        /// 依條件取得單筆資料
        /// </summary>
        T? Get(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 依條件取得多筆資料
        /// </summary>
        IEnumerable<T> GetMany(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 新增資料
        /// </summary>
        void Create(T entity);

        /// <summary>
        /// 更新資料
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// 刪除資料
        /// </summary>
        void Delete(T entity);

        /// <summary>
        /// 批次刪除
        /// </summary>
        void DeleteMany(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 檢查資料是否存在
        /// </summary>
        bool Exists(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 計算資料筆數
        /// </summary>
        int Count(Expression<Func<T, bool>> predicate);
    }
}
```

## GenericRepository 實作

放置路徑：Infrastructure/Repository/GenericRepository.cs

```csharp
using {Project}.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace {Project}.Infrastructure.Repository
{
    /// <summary>
    /// 泛型 Repository 實作
    /// </summary>
    /// <typeparam name="T">實體類型</typeparam>
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly DbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public IQueryable<T> GetAll()
        {
            return _dbSet.AsNoTracking();
        }

        public T? Get(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.AsNoTracking().FirstOrDefault(predicate);
        }

        public IEnumerable<T> GetMany(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.AsNoTracking().Where(predicate).ToList();
        }

        public void Create(T entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void DeleteMany(Expression<Func<T, bool>> predicate)
        {
            var entities = _dbSet.Where(predicate);
            _dbSet.RemoveRange(entities);
        }

        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Any(predicate);
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Count(predicate);
        }
    }
}
```

## 使用注意事項

1. **AsNoTracking**: 查詢時使用 `AsNoTracking()` 提升效能，避免不必要的變更追蹤
2. **延遲執行**: `GetAll()` 回傳 `IQueryable`，可在 Service 層組合更複雜的查詢
3. **特定 Repository**: 若需要複雜查詢邏輯，可繼承 `GenericRepository<T>` 建立特定 Repository
4. **交易管理**: Repository 不負責 SaveChanges，由 UnitOfWork 統一管理
5. **依賴注入**: 需在 Program.cs 註冊為 Scoped 服務

## 特定 Repository 範例

當需要特定實體的複雜查詢時：

```csharp
namespace {Project}.Infrastructure.Repository
{
    public interface I{Feature}Repository : IGenericRepository<{Entity}>
    {
        /// <summary>
        /// 取得啟用且有效的資料
        /// </summary>
        IEnumerable<{Entity}> GetActiveItems();
        
        /// <summary>
        /// 依關聯查詢
        /// </summary>
        {Entity}? GetWithRelated(string id);
    }

    public class {Feature}Repository : GenericRepository<{Entity}>, I{Feature}Repository
    {
        private readonly {Project}DbContext _context;

        public {Feature}Repository({Project}DbContext context) : base(context)
        {
            _context = context;
        }

        public IEnumerable<{Entity}> GetActiveItems()
        {
            return _context.{Entities}
                .AsNoTracking()
                .Where(x => x.Status == 1)
                .OrderByDescending(x => x.CreateDate)
                .ToList();
        }

        public {Entity}? GetWithRelated(string id)
        {
            return _context.{Entities}
                .AsNoTracking()
                .Include(x => x.{RelatedEntity})
                .FirstOrDefault(x => x.{Entity}ID == id);
        }
    }
}
```
