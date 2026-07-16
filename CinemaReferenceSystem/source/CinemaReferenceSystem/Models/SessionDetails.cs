namespace CinemaReferenceSystem.Models;

public class SessionDetails
{
    public int Id { get; set; }
    public string CinemaName { get; set; } = string.Empty;
    public int HallNumber { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
}