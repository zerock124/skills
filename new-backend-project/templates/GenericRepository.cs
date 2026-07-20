using System.Linq.Expressions;
using __PROJECT_NAME__.Domain.Interface;
using __PROJECT_NAME__.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace __PROJECT_NAME__.Infrastructure.Implement;

/// <summary>
/// 通用 Repository 實作，僅暫存變更，實際提交交由 <see cref="UnitOfWork"/> 處理
/// </summary>
/// <typeparam name="TEntity">資料實體型別</typeparam>
public class GenericRepository<TEntity>(EntityDbContext context) : IGenericRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// 取得指定實體的查詢來源
    /// </summary>
    /// <returns>可延遲查詢的實體集合</returns>
    public IQueryable<TEntity> GetAll() => context.Set<TEntity>().AsQueryable();

    /// <summary>
    /// 依指定條件取得單一實體
    /// </summary>
    /// <param name="predicate">查詢篩選條件</param>
    /// <returns>符合條件的實體；若查無資料則為 null</returns>
    public TEntity? Get(Expression<Func<TEntity, bool>> predicate)
        => context.Set<TEntity>().FirstOrDefault(predicate);

    /// <summary>
    /// 建立新的實體資料
    /// </summary>
    /// <param name="entity">要建立的實體</param>
    public void Create(TEntity entity) => context.Set<TEntity>().Add(entity);

    /// <summary>
    /// 更新既有的實體資料
    /// </summary>
    /// <param name="entity">要更新的實體</param>
    public void Update(TEntity entity) => context.Entry(entity).State = EntityState.Modified;

    /// <summary>
    /// 刪除指定的實體資料
    /// </summary>
    /// <param name="entity">要刪除的實體</param>
    public void Delete(TEntity entity) => context.Set<TEntity>().Remove(entity);
}

/// <summary>
/// Unit of Work 實作，統一管理交易範圍與資料提交
/// </summary>
public class UnitOfWork(EntityDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// 開始資料庫交易
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        // 巢狀呼叫時沿用既有交易，避免覆寫掉尚未提交的 transaction
        _transaction ??= await context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// 提交目前的資料庫交易
    /// </summary>
    public async Task CommitTransactionAsync()
    {
        if (_transaction is null) return;

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <summary>
    /// 復原目前的資料庫交易
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null) return;

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <summary>
    /// 儲存目前工作單元追蹤的所有資料異動
    /// </summary>
    /// <returns>受影響的資料筆數</returns>
    public Task<int> SaveChangesAsync() => context.SaveChangesAsync();
}
