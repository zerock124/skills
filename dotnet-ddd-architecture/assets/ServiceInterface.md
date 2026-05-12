# Service 介面樣板

放置路徑：Service/Interface/I{Feature}Service.cs

```csharp
using {Project}.Domain.DTOs.{Feature};
using {Project}.Domain.DTOs.Share;

namespace {Project}.Service.Interface
{
    public interface I{Feature}Service
    {
        Task<{Feature}DTO?> Get(string id);
        Task<PaginationDTO<{Feature}DTO>> GetList(Search{Feature}DTO search);
        Task<bool> Create(Edit{Feature}DTO data);
        Task<bool> Update(Edit{Feature}DTO data);
        Task<bool> Delete(string id);
    }
}
```
