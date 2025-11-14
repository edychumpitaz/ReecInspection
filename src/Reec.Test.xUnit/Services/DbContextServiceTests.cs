using Reec.Inspection.Services;
using Reec.Test.xUnit.Helpers;

namespace Reec.Test.xUnit.Services
{
    /// <summary>
    /// Tests para DbContextService - Gestión de contexto de base de datos
    /// </summary>
    public class DbContextServiceTests : IDisposable
    {
        private readonly TestInspectionDbContext _context;

        public DbContextServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext("DbContextServiceTests");
        }

        [Fact]
        public void Constructor_WithValidDbContext_ShouldCreateInstance()
        {
            // Act
            var service = new DbContextService<TestInspectionDbContext>(_context);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void GetDbContext_ShouldReturnSameInstance()
        {
            // Arrange
            var service = new DbContextService<TestInspectionDbContext>(_context);

            // Act
            var result1 = service.GetDbContext();
            var result2 = service.GetDbContext();

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1.Should().BeSameAs(result2);
            result1.Should().BeSameAs(_context);
        }

        [Fact]
        public void GetDbContext_ShouldReturnWorkingContext()
        {
            // Arrange
            var service = new DbContextService<TestInspectionDbContext>(_context);

            // Act
            var dbContext = service.GetDbContext();

            // Assert
            dbContext.Should().NotBeNull();
            dbContext.Database.Should().NotBeNull();
            dbContext.Database.CanConnect().Should().BeTrue();
        }

        public void Dispose()
        {
            TestDbContextFactory.CleanupContext(_context);
        }
    }
}
