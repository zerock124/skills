namespace __PROJECT_NAME__.Domain.DTOs.Share;

/// <summary>
/// API 統一回應基礎格式
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// 訊息內容
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 帶有資料內容的 API 統一回應格式
/// </summary>
/// <typeparam name="T">資料內容型別</typeparam>
public class DataResponse<T> : BaseResponse
{
    /// <summary>
    /// 回應資料內容
    /// </summary>
    public T? Data { get; set; }
}
