using To_Do_App_API.Controllers.DTOs;

namespace To_Do_App_API.Application.Interfaces.IServices
{
    public interface ITaskService
    {
        Task<List<TaskDto>> GetTasks(int userId, string status, DateTime? dueBefore, string sortBy, string order, string tag);
        Task<TaskDto> GetTask(int userId, int taskId);
        Task<TaskDto> CreateTask(int userId, CreateTaskDto createTaskDto);
        Task<bool> UpdateTask(int userId, int taskId, UpdateTaskDto updateTaskDto);
        Task<bool> DeleteTask(int userId, int taskId);
        Task<bool> CompleteTask(int userId, int taskId);
        Task<bool> ReopenTask(int userId, int taskId);
        Task<bool> ExtendDeadline(int userId, int taskId, ExtendDeadlineDto extendDeadlineDto);
        Task<bool> AddTags(int userId, int taskId, AddTagDto addTagDto);
        Task<bool> RemoveTag(int userId, int taskId, string tagName);
        Task<bool> AddComment(int userId, int taskId, CreateCommentDto addCommentDto);
        Task<bool> UpdateComment(int userId, int taskId, int commentId, CreateCommentDto updateCommentDto);
        Task<bool> DeleteComment(int userId, int taskId, int commentId);
        Task<List<CommentDto>> GetComments(int userId, int taskId);
        Task<CommentDto> GetComment(int userId, int taskId, int commentId);
    }
}
