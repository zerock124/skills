# AutoMapper Profile 樣板

## Service 層 Mapping

```csharp
using AutoMapper;
using {Project}.Domain.DTOs.{Feature};
using {Project}.Domain.Entities;

namespace {Project}.Service.Mappings
{
    public class {Feature}Mapping : Profile
    {
        public {Feature}Mapping()
        {
            CreateMap<{Entity}, {Feature}DTO>();
            CreateMap<Edit{Feature}DTO, {Entity}>();
        }
    }
}
```

## API 層 Mapping

```csharp
using AutoMapper;
using {Project}.API.Models.{Feature};
using {Project}.Domain.DTOs.{Feature};

namespace {Project}.API.Mappings
{
    public class {Feature}Mapping : Profile
    {
        public {Feature}Mapping()
        {
            CreateMap<Create{Feature}Request, Edit{Feature}DTO>();
            CreateMap<Update{Feature}Request, Edit{Feature}DTO>();
            CreateMap<Search{Feature}Request, Search{Feature}DTO>();
            CreateMap<{Feature}DTO, {Feature}Response>();
        }
    }
}
```
