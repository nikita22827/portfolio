namespace CinemaReferenceSystem.Models;

public class Hall : BaseEntity
{
    public int CinemaId { get; set; }
    public int HallNumber { get; set; }
    public int RowsCount { get; set; }
    public int MaxSeatsPerRow { get; set; }
}