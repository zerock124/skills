# Controller 樣板

放置路徑：API/Controllers/{Feature}Controller.cs

```csharp
using AutoMapper;
using {Project}.API.Models.{Feature};
using {Project}.API.Models.Shared;
using {Project}.Domain.DTOs.{Feature};
using {Project}.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace {Project}.API.Controllers
{
    public class {Feature}Controller : BaseController
    {
        private readonly IMapper _mapper;
        private readonly ILogger<{Feature}Controller> _logger;
        private readonly I{Feature}Service _service;

        public {Feature}Controller(
            ILogger<{Feature}Controller> logger,
            IMapper mapper,
            I{Feature}Service service)
        {
            _logger = logger;
            _mapper = mapper;
            _service = service;
        }

        [HttpGet(Name = "Get{Feature}")]
        public async Task<IActionResult> Get{Feature}([FromQuery] string id)
        {
            var res = new DataResponse<{Feature}Response>();
            try
            {
                var result = await _service.Get(id);
                if (result == null)
                {
                    res.Success = false;
                    res.Message = "無此資料";
                    return NotFound(res);
                }

                res.Success = true;
                res.Data = _mapper.Map<{Feature}Response>(result);
                return Ok(res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                res.Success = false;
                res.Message = "與伺服器連線發生錯誤";
                return InternalError(ex, res);
            }
        }
    }
}
```
