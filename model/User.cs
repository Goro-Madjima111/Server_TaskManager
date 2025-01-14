namespace MyAppDB.Models
{
    public class Users
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string? Fullname { get; set; }
        public DateOnly? Birthdate { get; set; } 
        public required string? Gender { get; set; } 
        public required string? Position { get; set; } 
        public required string? Department { get; set; } 
        public required string? Phonenumber { get; set; } 
        public required string? Photopath { get; set; } 

    }
}
