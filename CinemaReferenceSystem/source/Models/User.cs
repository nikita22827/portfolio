namespace CinemaReferenceSystem.Models;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
}