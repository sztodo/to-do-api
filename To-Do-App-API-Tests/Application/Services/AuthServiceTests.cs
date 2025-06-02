
using Moq;
using To_Do_App_API.Application.Services;
using To_Do_App_API.Infrastructure;
using Microsoft.Extensions.Configuration;
using To_Do_App_API.Controllers.DTOs;
using Microsoft.EntityFrameworkCore;
using To_Do_App_API.Infrastructure.Models;


namespace To_Do_App_API_Tests.Application.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _mockContext;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AuthService _authService;
        private const string KEY = "your-test-secret-key-with-minimum-length-requirement-min-512-test0yauon-popbECvhrtgbjmnmlomlkihnn**89";


        public AuthServiceTests()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            // Create actual context with in-memory database
            _mockContext = new ApplicationDbContext(options);

            // Setup configuration mock
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns(KEY);
            _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
            _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("test-audience");

            _authService = new AuthService(_mockContext, _mockConfiguration.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task RegisterUser_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var newUser = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "A!v3ryUn1qu3P@ssw0rd" + Guid.NewGuid().ToString("N"),
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _authService.Register(newUser);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Registration successful", result.Message);

            // Verify user was actually created in the database
            var createdUser = await _mockContext.Users.FirstOrDefaultAsync(u => u.Username == newUser.Username);
            Assert.NotNull(createdUser);
            Assert.Equal(newUser.Email, createdUser.Email);
        }

        [Fact]
        public async System.Threading.Tasks.Task RegisterUser_WithDuplicateUsername_ShouldFail()
        {
            // Arrange
            var existingUser = new User
            {
                Username = "testuser",
                Email = "existing@example.com",
                PasswordHash = "hash",
                FirstName = "Existing",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            await _mockContext.Users.AddAsync(existingUser);
            await _mockContext.SaveChangesAsync();

            var newUser = new RegisterDto
            {
                Username = "testuser",
                Email = "new@example.com",
                Password = "A!v3ryUn1qu3P@ssw0rd" + Guid.NewGuid().ToString("n"),
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _authService.Register(newUser);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Username already taken", result.Message);
        }

        [Fact]
        public async System.Threading.Tasks.Task RegisterUser_WithDuplicateEmail_ShouldFail()
        {
            // Arrange
            var existingUser = new User
            {
                Username = "existinguser",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "Existing",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            await _mockContext.Users.AddAsync(existingUser);
            await _mockContext.SaveChangesAsync();

            var newUser = new RegisterDto
            {
                Username = "newuser",
                Email = "test@example.com",
                Password = "A!v3ryUn1qu3P@ssw0rd" + Guid.NewGuid().ToString("N"),
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _authService.Register(newUser);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Email already registered", result.Message);
        }

        [Fact]
        public async System.Threading.Tasks.Task Login_WithValidCredentials_ShouldSucceed()
        {
            // Arrange
            var password = "A!v3ryUn1qu3P@ssw0rd" + Guid.NewGuid().ToString("D");
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = password,
                FirstName = "Test",
                LastName = "User"
            };
            await _authService.Register(registerDto);

            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = password
            };

            // Act
            var result = await _authService.Login(loginDto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.User);
            Assert.Equal(registerDto.Username, result.User.Username);
            Assert.Equal(registerDto.Email, result.User.Email);
        }

        [Fact]
        public async System.Threading.Tasks.Task Login_WithInvalidUsername_ShouldFail()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Username = "nonexistentuser",
                Password = "TestPassword123!"
            };

            // Act
            var result = await _authService.Login(loginDto);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Token);
            Assert.Null(result.User);
        }

        [Fact]
        public async System.Threading.Tasks.Task Login_WithInvalidPassword_ShouldFail()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "TestPassword123!",
                FirstName = "Test",
                LastName = "User"
            };
            await _authService.Register(registerDto);

            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = "WrongPassword123!"
            };

            // Act
            var result = await _authService.Login(loginDto);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Token);
            Assert.Null(result.User);
        }

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("test.email@domain.com")]
        [InlineData("test+label@example.com")]
        public async System.Threading.Tasks.Task Register_WithValidEmailFormats_ShouldSucceed(string email)
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Email = email,
                Password = "A!v3ryUn1qu3P@ssw0rd" + Guid.NewGuid().ToString("N"),
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _authService.Register(registerDto);

            // Assert
            Assert.True(result.Success);
        }


        public void Dispose()
        {
            // Cleanup
            _mockContext.Database.EnsureDeleted();
            _mockContext.Dispose();
        }

    }
}
