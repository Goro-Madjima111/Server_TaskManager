using System.ComponentModel.DataAnnotations;
public class TaskUser
{
    [Key]
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public int AssignedToUserId { get; set; }
    public User AssignedToUser { get; set; }
}
