namespace To_Do_App_API.Infrastructure.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<TaskTag> TaskTags { get; set; }
    }
}
