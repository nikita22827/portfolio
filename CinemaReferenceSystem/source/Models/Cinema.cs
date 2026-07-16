namespace CinemaReferenceSystem.Models;

public class Cinema : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double AverageRating { get; set; }
}