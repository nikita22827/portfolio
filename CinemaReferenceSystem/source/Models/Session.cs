namespace CinemaReferenceSystem.Models;

public class Session : BaseEntity
{
    public int HallId { get; set; }
    public int MovieId { get; set; }
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
}