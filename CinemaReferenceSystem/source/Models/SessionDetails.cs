namespace CinemaReferenceSystem.Models;

public class SessionDetails : BaseEntity
{
    public string CinemaName { get; set; } = string.Empty;
    public int HallNumber { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
}