# Service 實作樣板

放置路徑：Service/Implement/{Feature}Service.cs

```csharp
using AutoMapper;
using {Project}.Domain.DTOs.{Feature};
using {Project}.Domain.DTOs.Share;
using {Project}.Domain.Entities;
using {Project}.Domain.Interface;
using {Project}.Domain.UnitOfWork;
using {Project}.Service.Interface;
using Microsoft.Extensions.Logging;

namespace {Project}.Service.Implements
{
    public class {Feature}Service : I{Feature}Service
    {
        private readonly ILogger<{Feature}Service> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<{Entity}> _repository;

        public {Feature}Service(
            ILogger<{Feature}Service> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IGenericRepository<{Entity}> repository)
        {
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _repository = repository;
        }

        public async Task<{Feature}DTO?> Get(string id)
        {
            var entity = _repository.Get(p => p.{Entity}ID == id);
            if (entity == null) return null;
            return await Task.FromResult(_mapper.Map<{Feature}DTO>(entity));
        }

        public async Task<PaginationDTO<{Feature}DTO>> GetList(Search{Feature}DTO search)
        {
            var query = _repository.GetAll();
            var data = new PaginationDTO<{Feature}DTO>
            {
                CurrentPage = search.CurrentPage,
                PerPage = search.PerPage
            };

            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                query = query.Where(x => x.{Name}.Contains(search.Keyword));
            }

            data.TotalCounts = query.Count();
            data.DataList = _mapper.Map<List<{Feature}DTO>>(
                query.OrderByDescending(x => x.{Entity}ID)
                     .Skip((search.CurrentPage - 1) * search.PerPage)
                     .Take(search.PerPage)
                     .ToList());

            return await Task.FromResult(data);
        }

        public async Task<bool> Create(Edit{Feature}DTO data)
        {
            var entity = _mapper.Map<{Entity}>(data);
            _repository.Create(entity);
            return await Task.FromResult(true);
        }

        public async Task<bool> Update(Edit{Feature}DTO data)
        {
            var entity = _repository.Get(p => p.{Entity}ID == data.{Entity}ID);
            if (entity == null) return false;

            _mapper.Map(data, entity);
            _repository.Update(entity);
            return await Task.FromResult(true);
        }

        public async Task<bool> Delete(string id)
        {
            var entity = _repository.Get(p => p.{Entity}ID == id);
            if (entity == null) return false;

            _repository.Delete(entity);
            return await Task.FromResult(true);
        }
    }
}
```
