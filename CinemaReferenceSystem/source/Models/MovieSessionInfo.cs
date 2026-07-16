namespace CinemaReferenceSystem.Models;

public class MovieSessionInfo 
{
    public int SessionId { get; set; }
    public string CinemaName { get; set; } = string.Empty;
    public string CinemaAddress { get; set; } = string.Empty;
    public int CinemaId { get; set; }
    public double CinemaRating { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int HallNumber { get; set; }
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
}