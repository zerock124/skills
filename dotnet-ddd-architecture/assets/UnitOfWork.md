# Unit of Work 樣板

## IUnitOfWork 介面

放置路徑：Domain/UnitOfWork/IUnitOfWork.cs

```csharp
namespace {Project}.Domain.UnitOfWork
{
    /// <summary>
    /// Unit of Work 介面，負責交易管理
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// 儲存所有變更
        /// </summary>
        /// <returns>影響的資料筆數</returns>
        int SaveChanges();

        /// <summary>
        /// 非同步儲存所有變更
        /// </summary>
        /// <returns>影響的資料筆數</returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// 開始交易
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// 提交交易
        /// </summary>
        void Commit();

        /// <summary>
        /// 回滾交易
        /// </summary>
        void Rollback();
    }
}
```

## UnitOfWork 實作

放置路徑：Infrastructure/UnitOfWork/UnitOfWork.cs

```csharp
using {Project}.Domain.UnitOfWork;
using {Project}.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace {Project}.Infrastructure.UnitOfWork
{
    /// <summary>
    /// Unit of Work 實作
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly {Project}DbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork({Project}DbContext context)
        {
            _context = context;
        }

        public int SaveChanges()
        {
            try
            {
                return _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                // 記錄例外
                throw new Exception("資料儲存失敗", ex);
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // 記錄例外
                throw new Exception("資料儲存失敗", ex);
            }
        }

        public void BeginTransaction()
        {
            _transaction = _context.Database.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                _context.SaveChanges();
                _transaction?.Commit();
            }
            catch
            {
                Rollback();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
```

## 使用情境與範例

### 1. 一般 CRUD（不需明確交易）

```csharp
public async Task<bool> Create(Edit{Feature}DTO data)
{
    var entity = _mapper.Map<{Entity}>(data);
    _repository.Create(entity);
    
    // 由 UnitOfWork 統一儲存
    var result = await _unitOfWork.SaveChangesAsync();
    return result > 0;
}
```

### 2. 需要明確交易的複雜流程

```csharp
public async Task<bool> ComplexOperation(ComplexDTO data)
{
    try
    {
        _unitOfWork.BeginTransaction();

        // 操作 1
        var entity1 = _mapper.Map<Entity1>(data);
        _repository1.Create(entity1);

        // 操作 2
        var entity2 = _repository2.Get(x => x.ID == data.RelatedID);
        if (entity2 == null)
        {
            _unitOfWork.Rollback();
            return false;
        }
        entity2.Status = 0;
        _repository2.Update(entity2);

        // 操作 3
        var entities = _repository3.GetMany(x => x.ParentID == data.ID);
        _repository3.DeleteMany(x => x.ParentID == data.ID);

        // 統一提交
        _unitOfWork.Commit();
        return true;
    }
    catch (Exception ex)
    {
        _unitOfWork.Rollback();
        _logger.LogError(ex, "複雜操作失敗");
        throw;
    }
}
```

### 3. 搭配多個 Repository

```csharp
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<OrderItem> _orderItemRepository;
    private readonly IGenericRepository<Product> _productRepository;

    public OrderService(
        IUnitOfWork unitOfWork,
        IGenericRepository<Order> orderRepository,
        IGenericRepository<OrderItem> orderItemRepository,
        IGenericRepository<Product> productRepository)
    {
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _productRepository = productRepository;
    }

    public async Task<bool> CreateOrder(CreateOrderDTO data)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            // 建立訂單
            var order = new Order 
            { 
                OrderID = Guid.NewGuid().ToString(),
                CreateDate = DateTime.Now 
            };
            _orderRepository.Create(order);

            // 建立訂單明細並扣庫存
            foreach (var item in data.Items)
            {
                var product = _productRepository.Get(x => x.ProductID == item.ProductID);
                if (product == null || product.Stock < item.Quantity)
                {
                    _unitOfWork.Rollback();
                    return false;
                }

                // 扣庫存
                product.Stock -= item.Quantity;
                _productRepository.Update(product);

                // 新增訂單明細
                var orderItem = new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    Price = product.Price
                };
                _orderItemRepository.Create(orderItem);
            }

            _unitOfWork.Commit();
            return true;
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            throw new Exception("訂單建立失敗", ex);
        }
    }
}
```

## 注意事項

1. **何時使用交易**: 當一個操作涉及多個 Repository 且需要確保資料一致性時
2. **異常處理**: 使用 try-catch 包裹交易區塊，確保失敗時能 Rollback
3. **效能考量**: 交易會鎖定資源，應盡量縮短交易範圍
4. **一般操作**: 簡單的單一 CRUD 不需明確開交易，直接呼叫 `SaveChangesAsync()` 即可
5. **依賴注入**: 註冊為 Scoped，確保每個 HTTP 請求使用同一個 DbContext 實例
