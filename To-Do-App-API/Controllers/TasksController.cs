using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using To_Do_App_API.Controllers.DTOs;
using To_Do_App_API.Infrastructure.Models;
using To_Do_App_API.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace To_Do_App_API.Controllers
{
    [Authorize]
    [Route("api/tasks")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<IActionResult> GetTasks(
            [FromQuery] string status = null,
            [FromQuery] DateTime? dueBefore = null,
            [FromQuery] string sortBy = null,
            [FromQuery] string order = null,
            [FromQuery] string tag = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Start with the base query
            IQueryable<Infrastructure.Models.Task> query = _context.Tasks
                .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
                .Where(t => t.UserId == userId);

            // Apply filters
            if (status == "completed")
                query = query.Where(t => t.IsCompleted);
            else if (status == "active")
                query = query.Where(t => !t.IsCompleted);

            if (dueBefore.HasValue)
                query = query.Where(t => t.DueDate.HasValue && t.DueDate <= dueBefore);

            if (!string.IsNullOrEmpty(tag))
                query = query.Where(t => t.TaskTags.Any(tt => tt.Tag.Name == tag));

            // Apply sorting
            if (sortBy == "dueDate")
            {
                query = order?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate);
            }
            else
            {
                // Default sort by creation date
                query = query.OrderByDescending(t => t.CreatedAt);
            }

            // Execute query and map to DTOs
            var tasks = await query.Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt,
                DueDate = t.DueDate,
                Tags = t.TaskTags.Select(tt => tt.Tag.Name).ToList()
            }).ToListAsync();

            return Ok(tasks);
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks
                .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound();

            var taskDto = new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                IsCompleted = task.IsCompleted,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate,
                Tags = task.TaskTags.Select(tt => tt.Tag.Name).ToList()
            };

            return Ok(taskDto);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<IActionResult> CreateTask(CreateTaskDto createTaskDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = new Infrastructure.Models.Task
            {
                Title = createTaskDto.Title,
                Description = createTaskDto.Description,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                DueDate = createTaskDto.DueDate,
                UserId = userId,
                TaskTags = new List<TaskTag>()
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Add tags if provided
            if (createTaskDto.Tags != null && createTaskDto.Tags.Any())
            {
                foreach (var tagName in createTaskDto.Tags)
                {
                    var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                    if (tag == null)
                    {
                        tag = new Tag { Name = tagName };
                        _context.Tags.Add(tag);
                        await _context.SaveChangesAsync();
                    }

                    _context.TaskTags.Add(new TaskTag
                    {
                        TaskId = task.Id,
                        TagId = tag.Id
                    });
                }
                await _context.SaveChangesAsync();
            }

            var taskDto = new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                IsCompleted = task.IsCompleted,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate,
                Tags = createTaskDto.Tags ?? new List<string>()
            };

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, taskDto);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
                return NotFound();

            task.Title = updateTaskDto.Title ?? task.Title;
            task.Description = updateTaskDto.Description ?? task.Description;
            task.DueDate = updateTaskDto.DueDate ?? task.DueDate;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
                return NotFound();

            // Delete associated task tags
            var taskTags = await _context.TaskTags.Where(tt => tt.TaskId == id).ToListAsync();
            _context.TaskTags.RemoveRange(taskTags);

            // Delete associated comments
            var comments = await _context.Comments.Where(c => c.TaskId == id).ToListAsync();
            _context.Comments.RemoveRange(comments);

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/tasks/{id}/complete
        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> CompleteTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
                return NotFound();

            task.IsCompleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/tasks/{id}/reopen
        [HttpPatch("{id}/reopen")]
        public async Task<IActionResult> ReopenTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
                return NotFound();

            task.IsCompleted = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/tasks/{id}/extend-deadline
        [HttpPatch("{id}/extend-deadline")]
        public async Task<IActionResult> ExtendDeadline(int id, ExtendDeadlineDto extendDeadlineDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
                return NotFound();

            task.DueDate = extendDeadlineDto.NewDueDate;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/tasks/{id}/tags
        [HttpPost("{id}/tags")]
        public async Task<IActionResult> AddTags(int id, AddTagDto addTagDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks
                .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound();

            foreach (var tagName in addTagDto.Tags)
            {
                // Skip if tag already exists on task
                if (task.TaskTags.Any(tt => tt.Tag.Name == tagName))
                    continue;

                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                if (tag == null)
                {
                    tag = new Tag { Name = tagName };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }

                _context.TaskTags.Add(new TaskTag
                {
                    TaskId = task.Id,
                    TagId = tag.Id
                });
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/tasks/{id}/tags/{tagName}
        [HttpDelete("{id}/tags/{tagName}")]
        public async Task<IActionResult> RemoveTag(int id, string tagName)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
                return NotFound();

            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
            if (tag == null)
                return NotFound();

            var taskTag = await _context.TaskTags.FirstOrDefaultAsync(tt => tt.TaskId == id && tt.TagId == tag.Id);
            if (taskTag == null)
                return NotFound();

            _context.TaskTags.Remove(taskTag);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/tasks/{id}/comments
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, CreateCommentDto createCommentDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
                return NotFound();

            var comment = new Comment
            {
                Content = createCommentDto.Content,
                CreatedAt = DateTime.UtcNow,
                TaskId = id,
                UserId = userId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            var commentDto = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                Username = user.Username
            };

            return CreatedAtAction(nameof(GetComments), new { id = task.Id }, commentDto);
        }

        // GET: api/tasks/{id}/comments
        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
                return NotFound();

            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.TaskId == id)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UserId = c.UserId,
                    Username = c.User.Username
                })
                .ToListAsync();

            return Ok(comments);
        }
    }
}
