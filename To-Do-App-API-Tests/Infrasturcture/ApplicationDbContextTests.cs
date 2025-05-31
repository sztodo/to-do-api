using Microsoft.EntityFrameworkCore;
using To_Do_App_API.Infrastructure;
using To_Do_App_API.Infrastructure.Models;
using Xunit;

namespace To_Do_App_API_Tests.Infrasturcture
{
    public class ApplicationDbContextTests
    {
        private DbContextOptions<ApplicationDbContext> GetDbContextOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid().ToString()) // Ensure the method is accessible
                .Options;
        }

        [Fact]
        public void ApplicationDbContext_ConfiguresTaskTagCompositeKey()
        {
            // Arrange
            var options = GetDbContextOptions();

            // Act
            using var context = new ApplicationDbContext(options);
            var entityType = context.Model.FindEntityType(typeof(TaskTag));
            var primaryKey = entityType?.GetKeys().First();

            // Assert
            Assert.NotNull(primaryKey);
            Assert.Equal(2, primaryKey.Properties.Count);
            Assert.Contains(primaryKey.Properties, p => p.Name == "TaskId");
            Assert.Contains(primaryKey.Properties, p => p.Name == "TagId");
        }
    }
}
