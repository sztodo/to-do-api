namespace To_Do_App_API.Controllers.DTOs
{
    public class CreateTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public List<string> Tags { get; set; }
    }
}
