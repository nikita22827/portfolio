namespace CinemaReferenceSystem.Models;

public class Movie : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public int DurationMinutes { get; set; }
    public string? Director { get; set; }
    public string? PosterUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public string? Country { get; set; }
    public string? Description { get; set; }
    public DateTime? PremiereDate { get; set; }
    public int? ReleaseYear { get; set; }
    public double AverageRating { get; set; }
}