using Microsoft.EntityFrameworkCore;
using To_Do_App_API.Application.Interfaces.IServices;
using To_Do_App_API.Controllers.DTOs;
using To_Do_App_API.Infrastructure;
using To_Do_App_API.Infrastructure.Models;

namespace To_Do_App_API.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;

        public TaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskDto>> GetTasks(int userId, string status, DateTime? dueBefore, string sortBy, string order, string tag)
        {
            IQueryable<Infrastructure.Models.Task> query = _context.Tasks
                .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
                .Where(t => t.UserId == userId);

            if (status == "completed")
                query = query.Where(t => t.IsCompleted);
            else if (status == "active")
                query = query.Where(t => !t.IsCompleted);

            if (dueBefore.HasValue)
                query = query.Where(t => t.DueDate.HasValue && t.DueDate <= dueBefore);

            if (!string.IsNullOrEmpty(tag))
                query = query.Where(t => t.TaskTags.Any(tt => tt.Tag.Name == tag));

            if (sortBy == "dueDate")
            {
                query = order?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate);
            }
            else
            {
                query = query.OrderByDescending(t => t.CreatedAt);
            }

            return await query.Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt,
                DueDate = t.DueDate,
                Tags = t.TaskTags.Select(tt => tt.Tag.Name).ToList()
            }).ToListAsync();
        }

        public async Task<TaskDto> GetTask(int userId, int taskId)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
                return null;

            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                IsCompleted = task.IsCompleted,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate,
                Tags = task.TaskTags.Select(tt => tt.Tag.Name).ToList()
            };
        }

        public async Task<TaskDto> CreateTask(int userId, CreateTaskDto createTaskDto)
        {
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

            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                IsCompleted = task.IsCompleted,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate,
                Tags = createTaskDto.Tags ?? new List<string>()
            };
        }

        public async Task<bool> UpdateTask(int userId, int taskId, UpdateTaskDto updateTaskDto)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return false;

            task.Title = updateTaskDto.Title ?? task.Title;
            task.Description = updateTaskDto.Description ?? task.Description;
            task.DueDate = updateTaskDto.DueDate ?? task.DueDate;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTask(int userId, int taskId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteTask(int userId, int taskId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return false;

            task.IsCompleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReopenTask(int userId, int taskId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return false;

            task.IsCompleted = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExtendDeadline(int userId, int taskId, ExtendDeadlineDto extendDeadlineDto)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return false;

            task.DueDate = extendDeadlineDto.NewDueDate;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddTags(int userId, int taskId, AddTagDto addTagDto)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

            if (task == null)
                return false;

            foreach (var tagName in addTagDto.Tags)
            {
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
            return true;
        }

        public async Task<bool> RemoveTag(int userId, int taskId, string tagName)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return false;

            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
            if (tag == null)
                return false;

            var taskTag = await _context.TaskTags.FirstOrDefaultAsync(tt => tt.TaskId == taskId && tt.TagId == tag.Id);
            if (taskTag == null)
                return false;

            _context.TaskTags.Remove(taskTag);
            await _context.SaveChangesAsync();
            return true;
        }

        // Add to the existing TaskService.cs file

        public async Task<bool> AddComment(int userId, int taskId, CreateCommentDto addCommentDto)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return false;

            var comment = new Comment
            {
                Content = addCommentDto.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TaskId = taskId,
                UserId = userId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateComment(int userId, int taskId, int commentId, CreateCommentDto updateCommentDto)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId && c.UserId == userId);

            if (comment == null)
                return false;

            comment.Content = updateCommentDto.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteComment(int userId, int taskId, int commentId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId && c.UserId == userId);

            if (comment == null)
                return false;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CommentDto>> GetComments(int userId, int taskId)
        {
            // First check if the task exists and belongs to the user
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return new List<CommentDto>();

            return await _context.Comments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    UserId = c.UserId,
                    Username = c.User.Username
                })
                .ToListAsync();
        }
        public async Task<CommentDto> GetComment(int userId, int taskId, int commentId)
        {
            // First check if the task exists and belongs to the user
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
                return null;

            return await _context.Comments
                .Include(c => c.User)
                .Where(c => c.Id == commentId && c.TaskId == taskId)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    UserId = c.UserId,
                    Username = c.User.Username
                })
                .FirstOrDefaultAsync();
        }

    }
}
