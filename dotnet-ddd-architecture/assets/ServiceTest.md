# 單元測試樣板

放置路徑：tests/{Project}.Tests/{Feature}ServiceTests.cs

```csharp
using AutoMapper;
using {Project}.Domain.DTOs.{Feature};
using {Project}.Domain.Entities;
using {Project}.Domain.Interface;
using {Project}.Domain.UnitOfWork;
using {Project}.Service.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace {Project}.Tests
{
    public class {Feature}ServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IGenericRepository<{Entity}>> _mockRepository;
        private readonly {Feature}Service _service;

        public {Feature}ServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockRepository = new Mock<IGenericRepository<{Entity}>>();

            _service = new {Feature}Service(
                Mock.Of<ILogger<{Feature}Service>>(),
                _mockMapper.Object,
                _mockUnitOfWork.Object,
                _mockRepository.Object);
        }

        [Fact]
        public async Task Get_WhenEntityExists_ReturnsDTO()
        {
            var entity = new {Entity} { {Entity}ID = "test-id" };
            var dto = new {Feature}DTO { {Entity}ID = "test-id" };

            _mockRepository
                .Setup(r => r.Get(It.IsAny<Expression<Func<{Entity}, bool>>>()));

            _mockMapper
                .Setup(m => m.Map<{Feature}DTO>(entity))
                .Returns(dto);

            var result = await _service.Get("test-id");

            Assert.NotNull(result);
        }
    }
}
```
