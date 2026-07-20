using __PROJECT_NAME__.Domain.DTOs.Share;

namespace __PROJECT_NAME__.API.Models.Share;

/// <summary>
/// 分頁查詢的 API 統一回應格式
/// </summary>
/// <typeparam name="T">清單項目型別</typeparam>
public class PaginationResponse<T> : BaseResponse
{
    /// <summary>
    /// 分頁資料內容
    /// </summary>
    public Pagination<T> Data { get; set; } = new();

    /// <summary>
    /// 分頁資料結構
    /// </summary>
    /// <typeparam name="TItem">清單項目型別</typeparam>
    public class Pagination<TItem>
    {
        /// <summary>
        /// 每頁筆數
        /// </summary>
        public int PerPage { get; set; }

        /// <summary>
        /// 目前頁碼
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 符合查詢條件的總筆數
        /// </summary>
        public int TotalCounts { get; set; }

        /// <summary>
        /// 總頁數。無資料時回傳 1，避免前端分頁元件出現 0 頁的狀態
        /// </summary>
        public int TotalPage => TotalCounts > 0 && PerPage > 0
            ? (int)Math.Ceiling((double)TotalCounts / PerPage)
            : 1;

        /// <summary>
        /// 目前頁的資料清單
        /// </summary>
        public List<TItem> DataList { get; set; } = [];

        /// <summary>
        /// 報表頂端彙總合計（符合查詢條件的全部資料合計，非僅當頁）。不需要時為 null
        /// </summary>
        public object? Summary { get; set; }
    }
}
