using __PROJECT_NAME__.Domain.DTOs.Share;
using Microsoft.AspNetCore.Mvc;

namespace __PROJECT_NAME__.API.Controllers;

/// <summary>
/// 所有 Controller 的共用基底類別
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
[ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// 目前登入帳號的 AccountId（GUID 主鍵，對應 account_id Claim）
    /// </summary>
    protected Guid CurrentAccountId =>
        Guid.TryParse(User.FindFirst("account_id")?.Value, out var id) ? id : Guid.Empty;

    /// <summary>
    /// 目前登入帳號的帳號字串（對應 username Claim）
    /// </summary>
    protected string CurrentUsername => User.FindFirst("username")?.Value ?? string.Empty;

    /// <summary>
    /// 目前登入帳號的顯示名稱（對應 account_name Claim）
    /// </summary>
    protected string CurrentAccountName => User.FindFirst("account_name")?.Value ?? string.Empty;
}
