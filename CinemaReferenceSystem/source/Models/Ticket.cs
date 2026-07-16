namespace CinemaReferenceSystem.Models;

public class Ticket : BaseEntity
{
    public int SessionId { get; set; }
    public int RowNum { get; set; }
    public int SeatNum { get; set; }
    public bool IsSold { get; set; } = false;

}