namespace CinemaReferenceSystem.Models;

public class UserTicketDetail
{
    public int SessionId { get; set; }
    public string CinemaName { get; set; } = string.Empty;
    public int HallNumber { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
    public int RowNum { get; set; }
    public int SeatNum { get; set; }
    public int DisplaySeatNum { get; set; }
}