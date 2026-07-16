namespace CinemaReferenceSystem.Models;

public class Review : BaseEntity
{
    public int? CinemaId { get; set; }
    public int? MovieId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public int? CurrentUserVote { get; set; }
}