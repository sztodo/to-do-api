using Microsoft.EntityFrameworkCore;
using Moq;
using To_Do_App_API.Application.Services;
using To_Do_App_API.Controllers.DTOs;
using To_Do_App_API.Infrastructure;
using To_Do_App_API.Infrastructure.Models;

namespace To_Do_App_API_Tests.Application.Services
{
    public class TaskServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _mockContext;
        private readonly TaskService _taskService;
        private readonly int _testUserId = 1;
        private readonly string passwordHash = Guid.NewGuid().ToString();
        public TaskServiceTests()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _mockContext = new ApplicationDbContext(options);

            _taskService = new TaskService(_mockContext);
            _mockContext.Users.Add(new User
            {
                Id = _testUserId,
                Username = "testuser",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                FirstName = "Test",
                LastName = "Test",
                PasswordHash = passwordHash
            });
            _mockContext.SaveChanges();
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateTask_WithValidData_ShouldSucceed()
        {
            // Arrange
            var createTaskDto = new CreateTaskDto
            {
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Tags = new List<string> { "test", "important" }
            };

            // Act
            var result = await _taskService.CreateTask(_testUserId, createTaskDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createTaskDto.Title, result.Title);
            Assert.Equal(createTaskDto.Description, result.Description);
            Assert.Equal(createTaskDto.DueDate, result.DueDate);
            Assert.Contains("test", result.Tags);
            Assert.Contains("important", result.Tags);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTasks_ShouldReturnUserTasks()
        {
            // Arrange
            await CreateTestTask("Task 1");
            await CreateTestTask("Task 2");

            // Act
            var tasks = await _taskService.GetTasks(_testUserId, null, null, null, null, null);

            // Assert
            Assert.Equal(2, tasks.Count);
            Assert.All(tasks, task => Assert.Contains("Task", task.Title));
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateTask_WithValidData_ShouldSucceed()
        {
            // Arrange
            var task = await CreateTestTask("Original Task");
            var updateTaskDto = new UpdateTaskDto
            {
                Title = "Updated Task",
                Description = "Updated Description",
                DueDate = DateTime.UtcNow.AddDays(2)
            };

            // Act
            var result = await _taskService.UpdateTask(_testUserId, task.Id, updateTaskDto);
            var updatedTask = await _taskService.GetTask(_testUserId, task.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(updateTaskDto.Title, updatedTask.Title);
            Assert.Equal(updateTaskDto.Description, updatedTask.Description);
        }

        [Fact]
        public async System.Threading.Tasks.Task DeleteTask_ShouldRemoveTask()
        {
            // Arrange
            var task = await CreateTestTask("Task to Delete");

            // Act
            var result = await _taskService.DeleteTask(_testUserId, task.Id);
            var tasks = await _taskService.GetTasks(_testUserId, null, null, null, null, null);

            // Assert
            Assert.True(result);
            Assert.Empty(tasks);
        }

        [Fact]
        public async System.Threading.Tasks.Task AddTags_ShouldAddTagsToTask()
        {
            // Arrange
            var task = await CreateTestTask("Task with Tags");
            var addTagDto = new AddTagDto
            {
                Tags = new List<string> { "urgent", "important" }
            };

            // Act
            var result = await _taskService.AddTags(_testUserId, task.Id, addTagDto);
            var updatedTask = await _taskService.GetTask(_testUserId, task.Id);

            // Assert
            Assert.True(result);
            Assert.Contains("urgent", updatedTask.Tags);
            Assert.Contains("important", updatedTask.Tags);
        }

        [Fact]
        public async System.Threading.Tasks.Task RemoveTag_ShouldRemoveTagFromTask()
        {
            // Arrange
            var task = await CreateTestTask("Task with Tags");
            await _taskService.AddTags(_testUserId, task.Id, new AddTagDto
            {
                Tags = new List<string> { "urgent", "important" }
            });

            // Act
            var result = await _taskService.RemoveTag(_testUserId, task.Id, "urgent");
            var updatedTask = await _taskService.GetTask(_testUserId, task.Id);

            // Assert
            Assert.True(result);
            Assert.DoesNotContain("urgent", updatedTask.Tags);
            Assert.Contains("important", updatedTask.Tags);
        }

        [Fact]
        public async System.Threading.Tasks.Task AddComment_ShouldAddCommentToTask()
        {
            // Arrange
            var task = await CreateTestTask("Task with Comment");
            var commentDto = new CreateCommentDto { Content = "Test Comment" };

            // Act
            var result = await _taskService.AddComment(_testUserId, task.Id, commentDto);
            var comments = await _taskService.GetComments(_testUserId, task.Id);

            // Assert
            Assert.True(result);
            Assert.Single(comments);
            Assert.Equal(commentDto.Content, comments[0].Content);
        }

        private async Task<TaskDto> CreateTestTask(string title)
        {
            var createTaskDto = new CreateTaskDto
            {
                Title = title,
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            return await _taskService.CreateTask(_testUserId, createTaskDto);
        }

        public void Dispose()
        {
            _mockContext.Database.EnsureDeleted();
            _mockContext.Dispose();
        }
    }
}
