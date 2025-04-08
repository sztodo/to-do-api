using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using To_Do_App_API.Application.Interfaces.IServices;
using To_Do_App_API.Controllers.DTOs;

namespace To_Do_App_API.Controllers
{
    [Authorize]
    [Route("api/tasks")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks(
            [FromQuery] string status = null,
            [FromQuery] DateTime? dueBefore = null,
            [FromQuery] string sortBy = null,
            [FromQuery] string order = null,
            [FromQuery] string tag = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var tasks = await _taskService.GetTasks(userId, status, dueBefore, sortBy, order, tag);
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var task = await _taskService.GetTask(userId, id);
            if (task == null)
                return NotFound();

            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(CreateTaskDto createTaskDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var task = await _taskService.CreateTask(userId, createTaskDto);
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.UpdateTask(userId, id, updateTaskDto);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.DeleteTask(userId, id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> CompleteTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.CompleteTask(userId, id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPatch("{id}/reopen")]
        public async Task<IActionResult> ReopenTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.ReopenTask(userId, id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPatch("{id}/extend-deadline")]
        public async Task<IActionResult> ExtendDeadline(int id, ExtendDeadlineDto extendDeadlineDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.ExtendDeadline(userId, id, extendDeadlineDto);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/tags")]
        public async Task<IActionResult> AddTags(int id, AddTagDto addTagDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.AddTags(userId, id, addTagDto);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}/tags/{tagName}")]
        public async Task<IActionResult> RemoveTag(int id, string tagName)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.RemoveTag(userId, id, tagName);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, CreateCommentDto addCommentDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.AddComment(userId, id, addCommentDto);

            if (!success)
                return NotFound();

            // If you want to return the new comment in the response:
            var comments = await _taskService.GetComments(userId, id);
            var newComment = comments.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

            return CreatedAtAction(nameof(GetComments), new { id }, newComment);
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var comments = await _taskService.GetComments(userId, id);
            return Ok(comments);
        }

        // PUT: api/tasks/{id}/comments/{commentId}
        [HttpPut("{id}/comments/{commentId}")]
        public async Task<IActionResult> UpdateComment(int id, int commentId, CreateCommentDto updateCommentDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.UpdateComment(userId, id, commentId, updateCommentDto);

            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/tasks/{id}/comments/{commentId}
        [HttpDelete("{id}/comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(int id, int commentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var success = await _taskService.DeleteComment(userId, id, commentId);

            if (!success)
                return NotFound();

            return NoContent();
        }

        // GET: api/tasks/{id}/comments/{commentId}
        [HttpGet("{id}/comments/{commentId}")]
        public async Task<IActionResult> GetComment(int id, int commentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var comment = await _taskService.GetComment(userId, id, commentId);

            if (comment == null)
                return NotFound();

            return Ok(comment);
        }

    }
}
