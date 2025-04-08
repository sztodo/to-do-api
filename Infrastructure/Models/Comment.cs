namespace To_Do_App_API.Infrastructure.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public virtual Task Task { get; set; }
        public virtual User User { get; set; }
    }
}
