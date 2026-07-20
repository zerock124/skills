using System.Linq.Expressions;

namespace __PROJECT_NAME__.Domain.Interface;

/// <summary>
/// 通用 Repository 介面，提供各 Entity 共用的基本 CRUD 操作。
/// 刻意不提供 SaveChanges，資料提交一律經由 <see cref="IUnitOfWork"/>，
/// 確保同一次請求內跨多個 Repository 的變更落在同一個交易。
/// </summary>
/// <typeparam name="TEntity">資料實體型別</typeparam>
public interface IGenericRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// 取得指定實體的查詢來源
    /// </summary>
    /// <returns>可延遲查詢的實體集合</returns>
    IQueryable<TEntity> GetAll();

    /// <summary>
    /// 依指定條件取得單一實體
    /// </summary>
    /// <param name="predicate">查詢篩選條件</param>
    /// <returns>符合條件的實體；若查無資料則為 null</returns>
    TEntity? Get(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 建立新的實體資料
    /// </summary>
    /// <param name="entity">要建立的實體</param>
    void Create(TEntity entity);

    /// <summary>
    /// 更新既有的實體資料
    /// </summary>
    /// <param name="entity">要更新的實體</param>
    void Update(TEntity entity);

    /// <summary>
    /// 刪除指定的實體資料
    /// </summary>
    /// <param name="entity">要刪除的實體</param>
    void Delete(TEntity entity);
}

/// <summary>
/// Unit of Work 介面，統一管理同一次交易內的多個 Repository 與資料庫提交
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// 開始資料庫交易
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// 提交目前的資料庫交易
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// 復原目前的資料庫交易
    /// </summary>
    Task RollbackTransactionAsync();

    /// <summary>
    /// 儲存目前工作單元追蹤的所有資料異動
    /// </summary>
    /// <returns>受影響的資料筆數</returns>
    Task<int> SaveChangesAsync();
}
